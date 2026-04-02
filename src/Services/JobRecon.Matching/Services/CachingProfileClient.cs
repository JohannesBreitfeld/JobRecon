using System.Text.Json;
using JobRecon.Matching.Contracts;
using Microsoft.Extensions.Caching.Distributed;

namespace JobRecon.Matching.Services;

public sealed class CachingProfileClient(
    ProfileClient inner,
    IDistributedCache cache,
    ILogger<CachingProfileClient> logger) : IProfileClient
{
    private static readonly TimeSpan ProfileTtl = TimeSpan.FromMinutes(30);

    public async Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"profile:{userId}";

        try
        {
            var cached = await cache.GetAsync(key, cancellationToken);
            if (cached is not null)
            {
                logger.LogDebug("Profile cache hit for user {UserId}", userId);
                return JsonSerializer.Deserialize<ProfileDto>(cached);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis read failed for profile key {Key}", key);
        }

        var profile = await inner.GetProfileAsync(userId, cancellationToken);

        if (profile is not null)
        {
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(profile);
                await cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ProfileTtl
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis write failed for profile key {Key}", key);
            }
        }

        return profile;
    }
}
