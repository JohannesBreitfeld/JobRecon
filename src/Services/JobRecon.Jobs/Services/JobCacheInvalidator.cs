using StackExchange.Redis;

namespace JobRecon.Jobs.Services;

public sealed class JobCacheInvalidator(
    IConnectionMultiplexer redis,
    ILogger<JobCacheInvalidator> logger) : IJobCacheInvalidator
{
    private const string KeyPrefix = "JobRecon:";

    public async Task InvalidateJobDataAsync(CancellationToken cancellationToken = default)
    {
        var deleted = 0;
        foreach (var prefix in JobCacheKeys.InvalidationPrefixes)
        {
            deleted += await DeleteByPatternAsync($"{KeyPrefix}{prefix}*");
        }

        logger.LogInformation("Invalidated {Count} job cache entries", deleted);
    }

    public async Task InvalidateJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var key = $"{KeyPrefix}{JobCacheKeys.Detail(jobId)}";
        await db.KeyDeleteAsync(key);

        await DeleteByPatternAsync($"{KeyPrefix}search:*");
        await DeleteByPatternAsync($"{KeyPrefix}stats:*");
    }

    private async Task<int> DeleteByPatternAsync(string pattern)
    {
        var server = redis.GetServers().FirstOrDefault();
        if (server is null) return 0;

        var keys = server.Keys(pattern: pattern, pageSize: 250).ToArray();
        if (keys.Length == 0) return 0;

        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(keys);
        return keys.Length;
    }
}
