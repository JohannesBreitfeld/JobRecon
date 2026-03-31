using System.Security.Cryptography;
using System.Text;
using JobRecon.Domain.Common;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Jobs.Services;

public sealed class JobService : IJobService
{
    private readonly JobsDbContext _dbContext;
    private readonly ILogger<JobService> _logger;

    public JobService(JobsDbContext dbContext, ILogger<JobService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<JobSearchResponse>> SearchJobsAsync(
        Guid? userId,
        JobSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SalaryMin.HasValue && request.SalaryMax.HasValue && request.SalaryMin > request.SalaryMax)
            return Result.Failure<JobSearchResponse>(new Error("Jobs.InvalidSalaryRange", "SalaryMin cannot be greater than SalaryMax."));

        if (request.SalaryMin is < 0 || request.SalaryMax is < 0)
            return Result.Failure<JobSearchResponse>(new Error("Jobs.InvalidSalary", "Salary values must be non-negative."));

        if (request.ExperienceYearsMax is < 0)
            return Result.Failure<JobSearchResponse>(new Error("Jobs.InvalidExperience", "ExperienceYearsMax must be non-negative."));

        var query = _dbContext.Jobs
            .Include(j => j.Company)
            .Include(j => j.Tags)
            .Where(j => j.Status == JobStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchTerm = request.Query.ToLower();
            query = query.Where(j =>
                j.NormalizedTitle!.Contains(searchTerm) ||
                j.NormalizedDescription!.Contains(searchTerm) ||
                j.Company.NormalizedName!.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            var location = request.Location.ToLower();
            query = query.Where(j => j.NormalizedLocation != null && j.NormalizedLocation.Contains(location));
        }

        if (request.WorkLocationType.HasValue)
        {
            query = query.Where(j => j.WorkLocationType == request.WorkLocationType);
        }

        if (request.EmploymentType.HasValue)
        {
            query = query.Where(j => j.EmploymentType == request.EmploymentType);
        }

        if (request.SalaryMin.HasValue)
        {
            query = query.Where(j => j.SalaryMax >= request.SalaryMin || j.SalaryMax == null);
        }

        if (request.SalaryMax.HasValue)
        {
            query = query.Where(j => j.SalaryMin <= request.SalaryMax || j.SalaryMin == null);
        }

        if (request.CompanyId.HasValue)
        {
            query = query.Where(j => j.CompanyId == request.CompanyId);
        }

        if (request.JobSourceId.HasValue)
        {
            query = query.Where(j => j.JobSourceId == request.JobSourceId);
        }

        if (request.ExperienceYearsMax.HasValue)
        {
            query = query.Where(j => j.ExperienceYearsMin <= request.ExperienceYearsMax || j.ExperienceYearsMin == null);
        }

        if (request.PostedAfter.HasValue)
        {
            query = query.Where(j => j.PostedAt >= request.PostedAfter);
        }

        if (!string.IsNullOrWhiteSpace(request.Tags))
        {
            var normalizedTags = request.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLower()).ToList();
            query = query.Where(j => j.Tags.Any(t => normalizedTags.Contains(t.NormalizedName!)));
        }

        if (userId.HasValue && request.SavedOnly == true)
        {
            var savedJobIds = await _dbContext.SavedJobs
                .AsNoTracking()
                .Where(s => s.UserId == userId.Value)
                .Select(s => s.JobId)
                .ToListAsync(cancellationToken);

            query = query.Where(j => savedJobIds.Contains(j.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var sortDescending = request.SortDescending ?? true;
        var page = Math.Max(1, request.Page ?? 1);
        var pageSize = Math.Clamp(request.PageSize ?? 20, 1, 100);

        query = request.SortBy?.ToLower() switch
        {
            "salary" => sortDescending
                ? query.OrderByDescending(j => j.SalaryMax)
                : query.OrderBy(j => j.SalaryMin),
            "company" => sortDescending
                ? query.OrderByDescending(j => j.Company.Name)
                : query.OrderBy(j => j.Company.Name),
            "title" => sortDescending
                ? query.OrderByDescending(j => j.Title)
                : query.OrderBy(j => j.Title),
            _ => sortDescending
                ? query.OrderByDescending(j => j.PostedAt ?? j.CreatedAt)
                : query.OrderBy(j => j.PostedAt ?? j.CreatedAt)
        };

        var jobs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JobListResponse
            {
                Id = j.Id,
                Title = j.Title,
                Location = j.Location,
                WorkLocationType = j.WorkLocationType,
                EmploymentType = j.EmploymentType,
                SalaryMin = j.SalaryMin,
                SalaryMax = j.SalaryMax,
                SalaryCurrency = j.SalaryCurrency,
                PostedAt = j.PostedAt,
                CompanyName = j.Company.Name,
                CompanyLogoUrl = j.Company.LogoUrl,
                IsSaved = false,
                SavedStatus = null
            })
            .ToListAsync(cancellationToken);

        if (userId.HasValue && jobs.Count > 0)
        {
            var pageJobIds = jobs.Select(j => j.Id).ToList();
            var savedForPage = await _dbContext.SavedJobs
                .AsNoTracking()
                .Where(s => s.UserId == userId.Value && pageJobIds.Contains(s.JobId))
                .Select(s => new { s.JobId, s.Status })
                .ToDictionaryAsync(s => s.JobId, s => s.Status, cancellationToken);

            foreach (var job in jobs)
            {
                if (savedForPage.TryGetValue(job.Id, out var status))
                {
                    job.IsSaved = true;
                    job.SavedStatus = status;
                }
            }
        }

        return Result.Success(new JobSearchResponse
        {
            Jobs = jobs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    public async Task<Result<JobResponse>> GetJobAsync(
        Guid jobId,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .Include(j => j.Company)
            .Include(j => j.JobSource)
            .Include(j => j.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job is null)
        {
            return Result.Failure<JobResponse>(Error.NotFound("Job.NotFound", "Job not found"));
        }

        SavedJob? savedJob = null;
        if (userId.HasValue)
        {
            savedJob = await _dbContext.SavedJobs
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.JobId == jobId, cancellationToken);
        }

        return Result.Success(MapToResponse(job, savedJob));
    }

    public async Task<Result<JobResponse>> CreateJobAsync(
        CreateJobRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SalaryMin.HasValue && request.SalaryMax.HasValue && request.SalaryMin > request.SalaryMax)
            return Result.Failure<JobResponse>(new Error("Jobs.InvalidSalaryRange", "SalaryMin cannot be greater than SalaryMax."));

        if (request.SalaryMin is < 0 || request.SalaryMax is < 0)
            return Result.Failure<JobResponse>(new Error("Jobs.InvalidSalary", "Salary values must be non-negative."));

        if (request.ExperienceYearsMin.HasValue && request.ExperienceYearsMax.HasValue && request.ExperienceYearsMin > request.ExperienceYearsMax)
            return Result.Failure<JobResponse>(new Error("Jobs.InvalidExperienceRange", "ExperienceYearsMin cannot be greater than ExperienceYearsMax."));

        if (request.ExperienceYearsMin is < 0 || request.ExperienceYearsMax is < 0)
            return Result.Failure<JobResponse>(new Error("Jobs.InvalidExperience", "Experience years must be non-negative."));

        var manualSource = await _dbContext.JobSources
            .FirstOrDefaultAsync(s => s.Type == JobSourceType.Manual, cancellationToken);

        if (manualSource is null)
        {
            manualSource = new JobSource
            {
                Id = Guid.NewGuid(),
                Name = "Manual",
                Type = JobSourceType.Manual,
                IsEnabled = true
            };
            _dbContext.JobSources.Add(manualSource);
        }

        var normalizedCompanyName = request.CompanyName.ToLower().Trim();
        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(c => c.NormalizedName == normalizedCompanyName, cancellationToken);

        if (company is null)
        {
            company = new Company
            {
                Id = Guid.NewGuid(),
                Name = request.CompanyName,
                NormalizedName = normalizedCompanyName
            };
            _dbContext.Companies.Add(company);
        }

        var job = new Job
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            NormalizedTitle = request.Title.ToLower(),
            Description = request.Description,
            NormalizedDescription = request.Description?.ToLower(),
            Location = request.Location,
            NormalizedLocation = request.Location?.ToLower(),
            WorkLocationType = request.WorkLocationType,
            EmploymentType = request.EmploymentType,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            SalaryCurrency = request.SalaryCurrency ?? "SEK",
            SalaryPeriod = request.SalaryPeriod ?? "monthly",
            ExternalUrl = request.ExternalUrl,
            ApplicationUrl = request.ApplicationUrl,
            RequiredSkills = request.RequiredSkills,
            Benefits = request.Benefits,
            ExperienceYearsMin = request.ExperienceYearsMin,
            ExperienceYearsMax = request.ExperienceYearsMax,
            ExpiresAt = request.ExpiresAt,
            PostedAt = DateTime.UtcNow,
            Status = JobStatus.Active,
            CompanyId = company.Id,
            Company = company,
            JobSourceId = manualSource.Id,
            JobSource = manualSource
        };

        if (request.Tags is { Count: > 0 })
        {
            foreach (var tag in request.Tags)
            {
                job.Tags.Add(new JobTag
                {
                    Id = Guid.NewGuid(),
                    Name = tag,
                    NormalizedName = tag.ToLower(),
                    JobId = job.Id
                });
            }
        }

        job.Hash = ComputeJobHash(job.Title, job.Description, company.Name, job.Location, job.SalaryMin, job.SalaryMax);

        _dbContext.Jobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created job {JobId}: {Title} at {Company}", job.Id, job.Title, company.Name);

        return Result.Success(MapToResponse(job, null));
    }

    public async Task<Result<List<SavedJobResponse>>> GetSavedJobsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var savedJobs = await _dbContext.SavedJobs
            .Include(s => s.Job)
                .ThenInclude(j => j.Company)
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SavedAt)
            .ToListAsync(cancellationToken);

        var responses = savedJobs.Select(s => MapToSavedJobResponse(s, s.Job)).ToList();

        return Result.Success(responses);
    }

    public async Task<Result<SavedJobResponse>> SaveJobAsync(
        Guid userId,
        Guid jobId,
        SaveJobRequest request,
        CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .Include(j => j.Company)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job is null)
        {
            return Result.Failure<SavedJobResponse>(Error.NotFound("Job.NotFound", "Job not found"));
        }

        var existingSaved = await _dbContext.SavedJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.JobId == jobId, cancellationToken);

        if (existingSaved is not null)
        {
            return Result.Failure<SavedJobResponse>(Error.Conflict("SavedJob.AlreadyExists", "Job already saved"));
        }

        var savedJob = new SavedJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JobId = jobId,
            Notes = request.Notes,
            Status = SavedJobStatus.Saved,
            SavedAt = DateTime.UtcNow
        };

        _dbContext.SavedJobs.Add(savedJob);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} saved job {JobId}", userId, jobId);

        return Result.Success(MapToSavedJobResponse(savedJob, job));
    }

    public async Task<Result<SavedJobResponse>> UpdateSavedJobAsync(
        Guid userId,
        Guid jobId,
        UpdateSavedJobRequest request,
        CancellationToken cancellationToken = default)
    {
        var savedJob = await _dbContext.SavedJobs
            .Include(s => s.Job)
                .ThenInclude(j => j.Company)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.JobId == jobId, cancellationToken);

        if (savedJob is null)
        {
            return Result.Failure<SavedJobResponse>(Error.NotFound("SavedJob.NotFound", "Saved job not found"));
        }

        savedJob.Status = request.Status;
        savedJob.Notes = request.Notes ?? savedJob.Notes;
        savedJob.AppliedAt = request.AppliedAt ?? savedJob.AppliedAt;
        savedJob.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} updated saved job {JobId} to status {Status}", userId, jobId, request.Status);

        return Result.Success(MapToSavedJobResponse(savedJob, savedJob.Job));
    }

    public async Task<Result> RemoveSavedJobAsync(
        Guid userId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var savedJob = await _dbContext.SavedJobs
            .FirstOrDefaultAsync(s => s.UserId == userId && s.JobId == jobId, cancellationToken);

        if (savedJob is null)
        {
            return Result.Failure(Error.NotFound("SavedJob.NotFound", "Saved job not found"));
        }

        _dbContext.SavedJobs.Remove(savedJob);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} removed saved job {JobId}", userId, jobId);

        return Result.Success();
    }

    public async Task<Result<JobStatisticsResponse>> GetStatisticsAsync(
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);

        // Single query for all 4 counts using conditional aggregation
        var counts = await _dbContext.Jobs
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(j => j.Status == JobStatus.Active),
                Today = g.Count(j => j.CreatedAt >= today),
                ThisWeek = g.Count(j => j.CreatedAt >= weekAgo)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var savedJobsCount = userId.HasValue
            ? await _dbContext.SavedJobs.CountAsync(s => s.UserId == userId.Value, cancellationToken)
            : 0;

        var jobsBySourceTask = _dbContext.Jobs
            .Include(j => j.JobSource)
            .GroupBy(j => j.JobSource.Name)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Source, x => x.Count, cancellationToken);

        var jobsByLocationTask = _dbContext.Jobs
            .Where(j => j.Location != null)
            .GroupBy(j => j.Location!)
            .Select(g => new { Location = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToDictionaryAsync(x => x.Location, x => x.Count, cancellationToken);

        var jobsByTypeTask = _dbContext.Jobs
            .Where(j => j.EmploymentType != null)
            .GroupBy(j => j.EmploymentType!.Value)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, cancellationToken);

        await Task.WhenAll(jobsBySourceTask, jobsByLocationTask, jobsByTypeTask);

        return Result.Success(new JobStatisticsResponse
        {
            TotalJobs = counts?.Total ?? 0,
            ActiveJobs = counts?.Active ?? 0,
            NewJobsToday = counts?.Today ?? 0,
            NewJobsThisWeek = counts?.ThisWeek ?? 0,
            SavedJobsCount = savedJobsCount,
            JobsBySource = await jobsBySourceTask,
            JobsByLocation = await jobsByLocationTask,
            JobsByType = await jobsByTypeTask
        });
    }

    public async Task<Result<List<CompanyResponse>>> GetCompaniesAsync(
        string? search,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Companies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c => c.NormalizedName!.Contains(searchLower));
        }

        var companies = await query
            .OrderBy(c => c.Name)
            .Take(limit)
            .Select(c => new CompanyResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                LogoUrl = c.LogoUrl,
                Website = c.Website,
                Industry = c.Industry,
                Location = c.Location,
                EmployeeCount = c.EmployeeCount,
                JobCount = c.Jobs.Count(j => j.Status == JobStatus.Active)
            })
            .ToListAsync(cancellationToken);

        return Result.Success(companies);
    }

    public async Task<Result<CompanyResponse>> GetCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => new CompanyResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                LogoUrl = c.LogoUrl,
                Website = c.Website,
                Industry = c.Industry,
                Location = c.Location,
                EmployeeCount = c.EmployeeCount,
                JobCount = c.Jobs.Count(j => j.Status == JobStatus.Active)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
        {
            return Result.Failure<CompanyResponse>(Error.NotFound("Company.NotFound", "Company not found"));
        }

        return Result.Success(company);
    }

    public async Task<Result<List<string>>> GetTagsAsync(
        string? search,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.JobTags
            .Where(t => t.Job.Status == JobStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(t => t.NormalizedName!.Contains(searchLower));
        }

        var tags = await query
            .GroupBy(t => t.Name)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Take(limit)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);

        return Result.Success(tags);
    }

    private static SavedJobResponse MapToSavedJobResponse(SavedJob savedJob, Job job) => new()
    {
        Id = savedJob.Id,
        Status = savedJob.Status,
        Notes = savedJob.Notes,
        AppliedAt = savedJob.AppliedAt,
        SavedAt = savedJob.SavedAt,
        Job = MapToJobListResponse(job, isSaved: true, savedStatus: savedJob.Status)
    };

    private static JobListResponse MapToJobListResponse(Job job, bool isSaved, SavedJobStatus? savedStatus) => new()
    {
        Id = job.Id,
        Title = job.Title,
        Location = job.Location,
        WorkLocationType = job.WorkLocationType,
        EmploymentType = job.EmploymentType,
        SalaryMin = job.SalaryMin,
        SalaryMax = job.SalaryMax,
        SalaryCurrency = job.SalaryCurrency,
        PostedAt = job.PostedAt,
        CompanyName = job.Company.Name,
        CompanyLogoUrl = job.Company.LogoUrl,
        IsSaved = isSaved,
        SavedStatus = savedStatus
    };

    private static string ComputeJobHash(string? title, string? description, string? companyName, string? location, decimal? salaryMin, decimal? salaryMax)
    {
        var content = $"|{title}|{companyName}|{description}|{location}|{salaryMin}|{salaryMax}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLower();
    }

    private static JobResponse MapToResponse(Job job, SavedJob? savedJob)
    {
        return new JobResponse
        {
            Id = job.Id,
            Title = job.Title,
            Description = job.Description,
            Location = job.Location,
            WorkLocationType = job.WorkLocationType,
            EmploymentType = job.EmploymentType,
            SalaryMin = job.SalaryMin,
            SalaryMax = job.SalaryMax,
            SalaryCurrency = job.SalaryCurrency,
            SalaryPeriod = job.SalaryPeriod,
            ExternalUrl = job.ExternalUrl,
            ApplicationUrl = job.ApplicationUrl,
            RequiredSkills = job.RequiredSkills,
            Benefits = job.Benefits,
            ExperienceYearsMin = job.ExperienceYearsMin,
            ExperienceYearsMax = job.ExperienceYearsMax,
            PostedAt = job.PostedAt,
            ExpiresAt = job.ExpiresAt,
            Status = job.Status,
            Company = new CompanyResponse
            {
                Id = job.Company.Id,
                Name = job.Company.Name,
                Description = job.Company.Description,
                LogoUrl = job.Company.LogoUrl,
                Website = job.Company.Website,
                Industry = job.Company.Industry,
                Location = job.Company.Location,
                EmployeeCount = job.Company.EmployeeCount,
                JobCount = 0
            },
            SourceName = job.JobSource.Name,
            Tags = job.Tags.Select(t => t.Name).ToList(),
            IsSaved = savedJob is not null,
            SavedStatus = savedJob?.Status,
            CreatedAt = job.CreatedAt
        };
    }
}
