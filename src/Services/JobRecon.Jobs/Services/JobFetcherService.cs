using System.Security.Cryptography;
using System.Text;
using JobRecon.Contracts.Events;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace JobRecon.Jobs.Services;

public interface IJobFetcherService
{
    Task FetchJobsFromSourceAsync(Guid sourceId, CancellationToken cancellationToken = default);
    Task FetchAllJobsAsync(CancellationToken cancellationToken = default);
}

public sealed class JobFetcherService : IJobFetcherService
{
    private const int PersistBatchSize = 100;
    private const int MaxSaveRetries = 3;

    private readonly JobsDbContext _dbContext;
    private readonly IEnumerable<IJobFetcher> _fetchers;
    private readonly IJobEventPublisher _eventPublisher;
    private readonly ILogger<JobFetcherService> _logger;
    private readonly AsyncRetryPolicy _saveRetryPolicy;

    public JobFetcherService(
        JobsDbContext dbContext,
        IEnumerable<IJobFetcher> fetchers,
        IJobEventPublisher eventPublisher,
        ILogger<JobFetcherService> logger)
    {
        _dbContext = dbContext;
        _fetchers = fetchers;
        _eventPublisher = eventPublisher;
        _logger = logger;

        _saveRetryPolicy = Policy
            .Handle<DbUpdateException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(MaxSaveRetries, attempt =>
                TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (ex, delay, attempt, _) =>
                    _logger.LogWarning(ex, "SaveChangesAsync failed (attempt {Attempt}/{Max}), retrying in {Delay}",
                        attempt, MaxSaveRetries, delay));
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

            var totalJobCount = 0;
            var newJobCount = 0;
            var processedInBatch = 0;

            await foreach (var batch in fetcher.FetchJobBatchesAsync(source, cancellationToken))
            {
                foreach (var fetchedJob in batch.Jobs)
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
                    }
                    else
                    {
                        var company = await GetOrCreateCompanyAsync(fetchedJob, cancellationToken);
                        CreateJob(fetchedJob, jobHash, company.Id, sourceId);
                        newJobCount++;
                    }

                    totalJobCount++;
                    processedInBatch++;

                    // Persist in batches to limit memory usage
                    if (processedInBatch % PersistBatchSize == 0)
                    {
                        await SaveWithRetryAsync(cancellationToken);
                        _dbContext.ChangeTracker.Clear();

                        // Re-attach the source entity since we cleared the tracker
                        source = await _dbContext.JobSources
                            .FirstAsync(s => s.Id == sourceId, cancellationToken);
                    }
                }

                // Checkpoint: persist config after each date batch so we can resume
                if (batch.CheckpointConfig is not null)
                {
                    source.Configuration = batch.CheckpointConfig;
                }

                source.UpdatedAt = DateTime.UtcNow;
                await SaveWithRetryAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();

                source = await _dbContext.JobSources
                    .FirstAsync(s => s.Id == sourceId, cancellationToken);

                processedInBatch = 0;
            }

            source.LastFetchedAt = DateTime.UtcNow;
            source.LastFetchJobCount = totalJobCount;
            source.LastFetchError = null;
            source.UpdatedAt = DateTime.UtcNow;

            await SaveWithRetryAsync(cancellationToken);

            _logger.LogInformation(
                "Fetched {TotalCount} jobs from {SourceName}, {NewCount} new",
                totalJobCount, source.Name, newJobCount);

            // Notify Matching service so it can embed new jobs immediately
            if (newJobCount > 0)
            {
                await _eventPublisher.PublishJobsFetchedAsync(
                    new JobsFetchedIntegrationEvent(
                        Guid.NewGuid(), sourceId, totalJobCount, newJobCount, DateTime.UtcNow),
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching jobs from {SourceName}", source.Name);

            // Re-load source in case tracker was cleared during processing
            source = await _dbContext.JobSources
                .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

            if (source is not null)
            {
                source.LastFetchError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                source.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private void CreateJob(FetchedJob fetchedJob, string jobHash, Guid companyId, Guid sourceId)
    {
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
            CompanyId = companyId,
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
    }

    private async Task SaveWithRetryAsync(CancellationToken cancellationToken)
    {
        await _saveRetryPolicy.ExecuteAsync(ct =>
            _dbContext.SaveChangesAsync(ct), cancellationToken);
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
