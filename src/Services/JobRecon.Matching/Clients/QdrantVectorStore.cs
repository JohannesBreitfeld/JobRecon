using JobRecon.Matching.Configuration;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace JobRecon.Matching.Clients;

public sealed class QdrantVectorStore(
    QdrantClient client,
    IOptions<QdrantSettings> options,
    ILogger<QdrantVectorStore> logger) : IVectorStore
{
    private const string CollectionName = "job_embeddings";
    private readonly QdrantSettings _settings = options.Value;

    public async Task EnsureCollectionAsync(CancellationToken ct = default)
    {
        try
        {
            var collections = await client.ListCollectionsAsync(ct);
            if (collections.Any(c => c == CollectionName))
                return;

            await client.CreateCollectionAsync(
                CollectionName,
                new VectorParams
                {
                    Size = (ulong)_settings.VectorSize,
                    Distance = Distance.Cosine
                },
                cancellationToken: ct);

            logger.LogInformation("Created Qdrant collection {Collection} with vector size {Size}",
                CollectionName, _settings.VectorSize);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure Qdrant collection exists");
            throw;
        }
    }

    public async Task UpsertAsync(Guid jobId, float[] embedding, CancellationToken ct = default)
    {
        var point = new PointStruct
        {
            Id = new PointId { Uuid = jobId.ToString() },
            Vectors = embedding
        };

        await client.UpsertAsync(CollectionName, [point], cancellationToken: ct);
    }

    public async Task<List<VectorSearchResult>> SearchAsync(
        float[] queryVector, int limit, CancellationToken ct = default)
    {
        try
        {
            var results = await client.SearchAsync(
                CollectionName,
                queryVector,
                limit: (ulong)limit,
                cancellationToken: ct);

            return results
                .Where(r => r.Id.HasUuid)
                .Select(r => new VectorSearchResult(
                    Guid.Parse(r.Id.Uuid),
                    r.Score))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Vector search failed, returning empty results");
            return [];
        }
    }

    public async Task<bool> ExistsAsync(Guid jobId, CancellationToken ct = default)
    {
        try
        {
            var points = await client.RetrieveAsync(
                CollectionName,
                [new PointId { Uuid = jobId.ToString() }],
                withPayload: false,
                withVectors: false,
                cancellationToken: ct);

            return points.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<HashSet<Guid>> FilterExistingAsync(IEnumerable<Guid> jobIds, CancellationToken ct = default)
    {
        try
        {
            var pointIds = jobIds.Select(id => new PointId { Uuid = id.ToString() }).ToList();
            var points = await client.RetrieveAsync(
                CollectionName,
                pointIds,
                withPayload: false,
                withVectors: false,
                cancellationToken: ct);

            return points
                .Where(p => p.Id.HasUuid)
                .Select(p => Guid.Parse(p.Id.Uuid))
                .ToHashSet();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to batch check existing vectors, returning empty set");
            return [];
        }
    }
}
