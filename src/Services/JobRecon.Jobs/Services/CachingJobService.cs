using System.Text.Json;
using JobRecon.Domain.Common;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace JobRecon.Jobs.Services;

public sealed class CachingJobService(
    [FromKeyedServices("inner")] IJobService inner,
    IDistributedCache cache,
    IJobCacheInvalidator invalidator,
    JobsDbContext dbContext,
    ILogger<CachingJobService> logger) : IJobService
{
    private static readonly TimeSpan TagsTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan StatsTtl = TimeSpan.FromMinutes(20);
    private static readonly TimeSpan SearchTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CompaniesTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DetailTtl = TimeSpan.FromMinutes(30);

    public async Task<Result<List<string>>> GetTagsAsync(
        string? search,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var key = JobCacheKeys.Tags(search, limit);
        return await GetOrSetAsync(key, TagsTtl,
            () => inner.GetTagsAsync(search, limit, cancellationToken),
            cancellationToken);
    }

    public async Task<Result<JobStatisticsResponse>> GetStatisticsAsync(
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var key = JobCacheKeys.StatisticsGlobal();

        // Cache global stats (without per-user saved count)
        var result = await GetOrSetAsync(key, StatsTtl,
            () => inner.GetStatisticsAsync(null, cancellationToken),
            cancellationToken);

        if (result.IsFailure || !userId.HasValue)
            return result;

        // Overlay per-user saved count (cheap query, not worth caching)
        var savedCount = await dbContext.SavedJobs
            .CountAsync(s => s.UserId == userId.Value, cancellationToken);

        var stats = result.Value;
        return Result.Success(new JobStatisticsResponse
        {
            TotalJobs = stats.TotalJobs,
            ActiveJobs = stats.ActiveJobs,
            NewJobsToday = stats.NewJobsToday,
            NewJobsThisWeek = stats.NewJobsThisWeek,
            SavedJobsCount = savedCount,
            JobsBySource = stats.JobsBySource,
            JobsByLocation = stats.JobsByLocation,
            JobsByType = stats.JobsByType
        });
    }

    public async Task<Result<JobSearchResponse>> SearchJobsAsync(
        Guid? userId,
        JobSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        // User-specific saved-only queries bypass cache entirely
        if (request.SavedOnly == true)
            return await inner.SearchJobsAsync(userId, request, cancellationToken);

        var key = JobCacheKeys.Search(request);

        // Cache the global result (no userId)
        var result = await GetOrSetAsync(key, SearchTtl,
            () => inner.SearchJobsAsync(null, request, cancellationToken),
            cancellationToken);

        if (result.IsFailure || !userId.HasValue)
            return result;

        // Overlay per-user saved status
        return await OverlaySearchSavedStatusAsync(result.Value, userId.Value, cancellationToken);
    }

    public async Task<Result<JobResponse>> GetJobAsync(
        Guid jobId,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        var key = JobCacheKeys.Detail(jobId);

        // Cache the global result (no userId)
        var result = await GetOrSetAsync(key, DetailTtl,
            () => inner.GetJobAsync(jobId, null, cancellationToken),
            cancellationToken);

        if (result.IsFailure || !userId.HasValue)
            return result;

        // Overlay per-user saved status
        var savedJob = await dbContext.SavedJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.JobId == jobId, cancellationToken);

        if (savedJob is not null)
        {
            var job = result.Value;
            job.IsSaved = true;
            job.SavedStatus = savedJob.Status;
        }

        return result;
    }

    public async Task<Result<List<CompanyResponse>>> GetCompaniesAsync(
        string? search,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var key = JobCacheKeys.Companies(search, limit);
        return await GetOrSetAsync(key, CompaniesTtl,
            () => inner.GetCompaniesAsync(search, limit, cancellationToken),
            cancellationToken);
    }

    public async Task<Result<CompanyResponse>> GetCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var key = JobCacheKeys.Company(companyId);
        return await GetOrSetAsync(key, CompaniesTtl,
            () => inner.GetCompanyAsync(companyId, cancellationToken),
            cancellationToken);
    }

    // Write operations: delegate to inner, invalidate cache on success

    public async Task<Result<JobResponse>> CreateJobAsync(
        CreateJobRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.CreateJobAsync(request, cancellationToken);
        if (result.IsSuccess)
            await TryInvalidateAsync(() => invalidator.InvalidateJobDataAsync(cancellationToken));
        return result;
    }

    // User-specific operations: no caching needed

    public Task<Result<List<SavedJobResponse>>> GetSavedJobsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => inner.GetSavedJobsAsync(userId, cancellationToken);

    public Task<Result<SavedJobResponse>> SaveJobAsync(
        Guid userId,
        Guid jobId,
        SaveJobRequest request,
        CancellationToken cancellationToken = default)
        => inner.SaveJobAsync(userId, jobId, request, cancellationToken);

    public Task<Result<SavedJobResponse>> UpdateSavedJobAsync(
        Guid userId,
        Guid jobId,
        UpdateSavedJobRequest request,
        CancellationToken cancellationToken = default)
        => inner.UpdateSavedJobAsync(userId, jobId, request, cancellationToken);

    public Task<Result> RemoveSavedJobAsync(
        Guid userId,
        Guid jobId,
        CancellationToken cancellationToken = default)
        => inner.RemoveSavedJobAsync(userId, jobId, cancellationToken);

    // Cache-aside helper with graceful degradation

    private async Task<Result<T>> GetOrSetAsync<T>(
        string key,
        TimeSpan ttl,
        Func<Task<Result<T>>> factory,
        CancellationToken cancellationToken)
    {
        try
        {
            var cached = await cache.GetAsync(key, cancellationToken);
            if (cached is not null)
            {
                var value = JsonSerializer.Deserialize<T>(cached);
                if (value is not null)
                    return Result.Success(value);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis cache read failed for key {Key}, falling through to database", key);
        }

        var result = await factory();

        if (result.IsSuccess)
        {
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(result.Value);
                await cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis cache write failed for key {Key}", key);
            }
        }

        return result;
    }

    private async Task<Result<JobSearchResponse>> OverlaySearchSavedStatusAsync(
        JobSearchResponse response,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (response.Jobs.Count == 0)
            return Result.Success(response);

        var jobIds = response.Jobs.Select(j => j.Id).ToList();
        var savedForPage = await dbContext.SavedJobs
            .AsNoTracking()
            .Where(s => s.UserId == userId && jobIds.Contains(s.JobId))
            .Select(s => new { s.JobId, s.Status })
            .ToDictionaryAsync(s => s.JobId, s => s.Status, cancellationToken);

        foreach (var job in response.Jobs)
        {
            if (savedForPage.TryGetValue(job.Id, out var status))
            {
                job.IsSaved = true;
                job.SavedStatus = status;
            }
        }

        return Result.Success(response);
    }

    private async Task TryInvalidateAsync(Func<Task> invalidation)
    {
        try
        {
            await invalidation();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache invalidation failed");
        }
    }
}
