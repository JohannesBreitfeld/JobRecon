using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace JobRecon.Matching.Clients;

public sealed class CachingOllamaClient(
    OllamaClient inner,
    IDistributedCache cache,
    ILogger<CachingOllamaClient> logger) : IOllamaClient
{
    private static readonly TimeSpan EmbeddingTtl = TimeSpan.FromHours(24);

    public async Task<float[]?> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLower();
        var key = $"ollama:embed:{hash}";

        try
        {
            var cached = await cache.GetAsync(key, ct);
            if (cached is not null)
            {
                var deserialized = JsonSerializer.Deserialize<float[]>(cached);
                if (deserialized is { Length: > 0 })
                {
                    logger.LogDebug("Embedding cache hit for key {Key}", key);
                    return deserialized;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis read failed for embedding key {Key}", key);
        }

        var embedding = await inner.GetEmbeddingAsync(text, ct);

        if (embedding is not null)
        {
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(embedding);
                await cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = EmbeddingTtl
                }, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis write failed for embedding key {Key}", key);
            }
        }

        return embedding;
    }
}
