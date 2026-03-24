using System.Security.Cryptography;
using System.Text;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Jobs.Services;

public interface IJobFetcherService
{
    Task FetchJobsFromSourceAsync(Guid sourceId, CancellationToken cancellationToken = default);
    Task FetchAllJobsAsync(CancellationToken cancellationToken = default);
}

public sealed class JobFetcherService : IJobFetcherService
{
    private readonly JobsDbContext _dbContext;
    private readonly IEnumerable<IJobFetcher> _fetchers;
    private readonly ILogger<JobFetcherService> _logger;

    public JobFetcherService(
        JobsDbContext dbContext,
        IEnumerable<IJobFetcher> fetchers,
        ILogger<JobFetcherService> logger)
    {
        _dbContext = dbContext;
        _fetchers = fetchers;
        _logger = logger;
    }

    public async Task FetchAllJobsAsync(CancellationToken cancellationToken = default)
    {
        var sources = await _dbContext.JobSources
            .AsNoTracking()
            .Where(s => s.IsEnabled)
            .Where(s => s.LastFetchedAt == null ||
                s.LastFetchedAt < DateTime.UtcNow.AddMinutes(-s.FetchIntervalMinutes))
            .ToListAsync(cancellationToken);

        foreach (var source in sources)
        {
            try
            {
                await FetchJobsFromSourceAsync(source.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching jobs from source {SourceId}", source.Id);
            }
        }
    }

    public async Task FetchJobsFromSourceAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        var source = await _dbContext.JobSources
            .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

        if (source is null)
        {
            _logger.LogWarning("Job source {SourceId} not found", sourceId);
            return;
        }

        var fetcher = _fetchers.FirstOrDefault(f => f.SourceType == source.Type);

        if (fetcher is null)
        {
            _logger.LogWarning("No fetcher found for source type {Type}", source.Type);
            source.LastFetchError = $"No fetcher available for type {source.Type}";
            source.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            _logger.LogInformation("Starting job fetch from {SourceName}", source.Name);

            var fetchedJobs = await fetcher.FetchJobsAsync(source, cancellationToken);
            var newJobCount = 0;
            var processedCount = 0;
            const int persistBatchSize = 100;

            foreach (var fetchedJob in fetchedJobs)
            {
                var jobHash = ComputeHash(fetchedJob);
                var existingJob = await _dbContext.Jobs
                    .FirstOrDefaultAsync(j =>
                        j.JobSourceId == sourceId &&
                        (j.ExternalId == fetchedJob.ExternalId || j.Hash == jobHash),
                        cancellationToken);

                if (existingJob is not null)
                {
                    if (existingJob.Hash != jobHash)
                    {
                        UpdateJob(existingJob, fetchedJob, jobHash);
                    }
                    processedCount++;
                }
                else
                {
                    var company = await GetOrCreateCompanyAsync(fetchedJob, cancellationToken);

                    var job = new Job
                    {
                        Id = Guid.NewGuid(),
                        Title = fetchedJob.Title,
                        NormalizedTitle = fetchedJob.Title.ToLower(),
                        Description = fetchedJob.Description,
                        Location = fetchedJob.Location,
                        WorkLocationType = fetchedJob.WorkLocationType,
                        EmploymentType = fetchedJob.EmploymentType,
                        SalaryMin = fetchedJob.SalaryMin,
                        SalaryMax = fetchedJob.SalaryMax,
                        SalaryCurrency = fetchedJob.SalaryCurrency ?? "SEK",
                        SalaryPeriod = fetchedJob.SalaryPeriod,
                        ExternalId = fetchedJob.ExternalId,
                        ExternalUrl = fetchedJob.ExternalUrl,
                        ApplicationUrl = fetchedJob.ApplicationUrl,
                        RequiredSkills = fetchedJob.RequiredSkills,
                        Benefits = fetchedJob.Benefits,
                        ExperienceYearsMin = fetchedJob.ExperienceYearsMin,
                        ExperienceYearsMax = fetchedJob.ExperienceYearsMax,
                        PostedAt = fetchedJob.PostedAt ?? DateTime.UtcNow,
                        ExpiresAt = fetchedJob.ExpiresAt,
                        Status = JobStatus.Active,
                        Hash = jobHash,
                        CompanyId = company.Id,
                        JobSourceId = sourceId
                    };

                    foreach (var tag in fetchedJob.Tags)
                    {
                        job.Tags.Add(new JobTag
                        {
                            Id = Guid.NewGuid(),
                            Name = tag,
                            NormalizedName = tag.ToLower(),
                            JobId = job.Id
                        });
                    }

                    _dbContext.Jobs.Add(job);
                    newJobCount++;
                    processedCount++;
                }

                // Persist in batches to limit memory usage
                if (processedCount % persistBatchSize == 0)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    _dbContext.ChangeTracker.Clear();

                    // Re-attach the source entity since we cleared the tracker
                    source = await _dbContext.JobSources
                        .FirstAsync(s => s.Id == sourceId, cancellationToken);
                }
            }

            source.LastFetchedAt = DateTime.UtcNow;
            source.LastFetchJobCount = fetchedJobs.Count;
            source.LastFetchError = null;
            source.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Fetched {TotalCount} jobs from {SourceName}, {NewCount} new",
                fetchedJobs.Count, source.Name, newJobCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching jobs from {SourceName}", source.Name);

            source.LastFetchError = ex.Message;
            source.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<Company> GetOrCreateCompanyAsync(FetchedJob fetchedJob, CancellationToken cancellationToken)
    {
        var normalizedName = fetchedJob.CompanyName.ToLower().Trim();

        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(c => c.NormalizedName == normalizedName, cancellationToken);

        if (company is null)
        {
            company = new Company
            {
                Id = Guid.NewGuid(),
                Name = fetchedJob.CompanyName,
                NormalizedName = normalizedName,
                LogoUrl = fetchedJob.CompanyLogoUrl,
                Website = fetchedJob.CompanyWebsite
            };
            _dbContext.Companies.Add(company);
        }
        else if (fetchedJob.CompanyLogoUrl is not null && company.LogoUrl is null)
        {
            company.LogoUrl = fetchedJob.CompanyLogoUrl;
            company.UpdatedAt = DateTime.UtcNow;
        }

        return company;
    }

    private static void UpdateJob(Job job, FetchedJob fetchedJob, string hash)
    {
        job.Title = fetchedJob.Title;
        job.NormalizedTitle = fetchedJob.Title.ToLower();
        job.Description = fetchedJob.Description;
        job.Location = fetchedJob.Location;
        job.WorkLocationType = fetchedJob.WorkLocationType;
        job.EmploymentType = fetchedJob.EmploymentType;
        job.SalaryMin = fetchedJob.SalaryMin;
        job.SalaryMax = fetchedJob.SalaryMax;
        job.SalaryCurrency = fetchedJob.SalaryCurrency;
        job.SalaryPeriod = fetchedJob.SalaryPeriod;
        job.ExternalUrl = fetchedJob.ExternalUrl;
        job.ApplicationUrl = fetchedJob.ApplicationUrl;
        job.RequiredSkills = fetchedJob.RequiredSkills;
        job.Benefits = fetchedJob.Benefits;
        job.ExperienceYearsMin = fetchedJob.ExperienceYearsMin;
        job.ExperienceYearsMax = fetchedJob.ExperienceYearsMax;
        job.ExpiresAt = fetchedJob.ExpiresAt;
        job.Hash = hash;
        job.UpdatedAt = DateTime.UtcNow;
    }

    private static string ComputeHash(FetchedJob job)
    {
        var content = $"{job.ExternalId}|{job.Title}|{job.CompanyName}|{job.Description}|{job.Location}|{job.SalaryMin}|{job.SalaryMax}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLower();
    }
}
