using Hangfire;
using JobRecon.Domain.Common;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Jobs.Services;

public sealed class JobSourceService : IJobSourceService
{
    private readonly JobsDbContext _dbContext;
    private readonly ILogger<JobSourceService> _logger;

    public JobSourceService(JobsDbContext dbContext, ILogger<JobSourceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<List<JobSourceResponse>>> GetJobSourcesAsync(
        CancellationToken cancellationToken = default)
    {
        var sources = await _dbContext.JobSources
            .OrderBy(s => s.Name)
            .Select(s => new JobSourceResponse
            {
                Id = s.Id,
                Name = s.Name,
                Type = s.Type,
                IsEnabled = s.IsEnabled,
                FetchIntervalMinutes = s.FetchIntervalMinutes,
                LastFetchedAt = s.LastFetchedAt,
                LastFetchJobCount = s.LastFetchJobCount,
                LastFetchError = s.LastFetchError
            })
            .ToListAsync(cancellationToken);

        return Result.Success(sources);
    }

    public async Task<Result<JobSourceResponse>> GetJobSourceAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var source = await _dbContext.JobSources
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (source is null)
        {
            return Result.Failure<JobSourceResponse>(Error.NotFound("JobSource.NotFound", "Job source not found"));
        }

        return Result.Success(new JobSourceResponse
        {
            Id = source.Id,
            Name = source.Name,
            Type = source.Type,
            IsEnabled = source.IsEnabled,
            FetchIntervalMinutes = source.FetchIntervalMinutes,
            LastFetchedAt = source.LastFetchedAt,
            LastFetchJobCount = source.LastFetchJobCount,
            LastFetchError = source.LastFetchError
        });
    }

    public async Task<Result<JobSourceResponse>> CreateJobSourceAsync(
        CreateJobSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingSource = await _dbContext.JobSources
            .FirstOrDefaultAsync(s => s.Name == request.Name, cancellationToken);

        if (existingSource is not null)
        {
            return Result.Failure<JobSourceResponse>(Error.Conflict("JobSource.AlreadyExists", "Job source with this name already exists"));
        }

        var source = new JobSource
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Type = request.Type,
            BaseUrl = request.BaseUrl,
            ApiKey = request.ApiKey,
            Configuration = request.Configuration,
            FetchIntervalMinutes = request.FetchIntervalMinutes,
            IsEnabled = true
        };

        _dbContext.JobSources.Add(source);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created job source {SourceId}: {Name} ({Type})", source.Id, source.Name, source.Type);

        return Result.Success(new JobSourceResponse
        {
            Id = source.Id,
            Name = source.Name,
            Type = source.Type,
            IsEnabled = source.IsEnabled,
            FetchIntervalMinutes = source.FetchIntervalMinutes,
            LastFetchedAt = source.LastFetchedAt,
            LastFetchJobCount = source.LastFetchJobCount,
            LastFetchError = source.LastFetchError
        });
    }

    public async Task<Result<JobSourceResponse>> UpdateJobSourceAsync(
        Guid id,
        UpdateJobSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var source = await _dbContext.JobSources
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (source is null)
        {
            return Result.Failure<JobSourceResponse>(Error.NotFound("JobSource.NotFound", "Job source not found"));
        }

        if (request.Name is not null && request.Name != source.Name)
        {
            var existingSource = await _dbContext.JobSources
                .FirstOrDefaultAsync(s => s.Name == request.Name && s.Id != id, cancellationToken);

            if (existingSource is not null)
            {
                return Result.Failure<JobSourceResponse>(Error.Conflict("JobSource.NameTaken", "Job source with this name already exists"));
            }

            source.Name = request.Name;
        }

        source.BaseUrl = request.BaseUrl ?? source.BaseUrl;
        source.ApiKey = request.ApiKey ?? source.ApiKey;
        source.Configuration = request.Configuration ?? source.Configuration;
        source.IsEnabled = request.IsEnabled ?? source.IsEnabled;
        source.FetchIntervalMinutes = request.FetchIntervalMinutes ?? source.FetchIntervalMinutes;
        source.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated job source {SourceId}", id);

        return Result.Success(new JobSourceResponse
        {
            Id = source.Id,
            Name = source.Name,
            Type = source.Type,
            IsEnabled = source.IsEnabled,
            FetchIntervalMinutes = source.FetchIntervalMinutes,
            LastFetchedAt = source.LastFetchedAt,
            LastFetchJobCount = source.LastFetchJobCount,
            LastFetchError = source.LastFetchError
        });
    }

    public async Task<Result> DeleteJobSourceAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var source = await _dbContext.JobSources
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (source is null)
        {
            return Result.Failure(Error.NotFound("JobSource.NotFound", "Job source not found"));
        }

        var hasJobs = await _dbContext.Jobs.AnyAsync(j => j.JobSourceId == id, cancellationToken);

        if (hasJobs)
        {
            return Result.Failure(Error.Validation("JobSource.HasJobs", "Cannot delete job source with existing jobs"));
        }

        _dbContext.JobSources.Remove(source);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted job source {SourceId}", id);

        return Result.Success();
    }

    public async Task<Result> TriggerFetchAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var source = await _dbContext.JobSources
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (source is null)
        {
            return Result.Failure(Error.NotFound("JobSource.NotFound", "Job source not found"));
        }

        if (!source.IsEnabled)
        {
            return Result.Failure(Error.Validation("JobSource.Disabled", "Job source is disabled"));
        }

        BackgroundJob.Enqueue<IJobFetcherService>(x => x.FetchJobsFromSourceAsync(id, CancellationToken.None));

        _logger.LogInformation("Triggered manual fetch for job source {SourceId}", id);

        return Result.Success();
    }
}
