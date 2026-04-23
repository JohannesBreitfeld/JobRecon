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
    private const string GeoFieldName = "location";
    private const int PayloadBatchSize = 500;
    private readonly QdrantSettings _settings = options.Value;

    public async Task EnsureCollectionAsync(CancellationToken ct = default)
    {
        try
        {
            var collections = await client.ListCollectionsAsync(ct);
            if (collections.Any(c => c == CollectionName))
            {
                await EnsureGeoIndexAsync(ct);
                return;
            }

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

            await EnsureGeoIndexAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure Qdrant collection exists");
            throw;
        }
    }

    public async Task UpsertAsync(Guid jobId, float[] embedding, GeoPayload? geoPayload = null, CancellationToken ct = default)
    {
        var point = new PointStruct
        {
            Id = new PointId { Uuid = jobId.ToString() },
            Vectors = embedding
        };

        if (geoPayload is not null)
        {
            point.Payload[GeoFieldName] = BuildGeoPointValue(geoPayload);
        }

        await client.UpsertAsync(CollectionName, [point], cancellationToken: ct);
    }

    public async Task<List<VectorSearchResult>> SearchAsync(
        float[] queryVector, int limit, IReadOnlyList<GeoCircle>? geoFilter = null, CancellationToken ct = default)
    {
        try
        {
            Filter? filter = null;
            if (geoFilter is { Count: > 0 })
            {
                filter = new Filter();
                foreach (var circle in geoFilter)
                {
                    filter.Should.Add(Conditions.GeoRadius(
                        GeoFieldName,
                        circle.Latitude,
                        circle.Longitude,
                        (float)(circle.RadiusKm * 1000))); // Qdrant expects meters
                }
            }

            var results = await client.SearchAsync(
                CollectionName,
                queryVector,
                filter: filter,
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

    public async Task SetGeoPayloadBatchAsync(
        IReadOnlyList<(Guid JobId, GeoPayload Payload)> items, CancellationToken ct = default)
    {
        for (var i = 0; i < items.Count; i += PayloadBatchSize)
        {
            var batch = items.Skip(i).Take(PayloadBatchSize).ToList();
            var guids = batch.Select(b => b.JobId).ToList();

            // All items in a batch get their own payload, but SetPayloadAsync applies the same payload to all IDs.
            // Group by unique lat/lng is impractical, so we process one at a time for correctness.
            // However, most jobs in a batch will have different coords, so use individual updates.
            foreach (var (jobId, payload) in batch)
            {
                var payloadDict = new Dictionary<string, Value>
                {
                    [GeoFieldName] = BuildGeoPointValue(payload)
                };

                await client.SetPayloadAsync(CollectionName, payloadDict, jobId, cancellationToken: ct);
            }

            if (i + PayloadBatchSize < items.Count)
            {
                logger.LogDebug("Geo payload backfill progress: {Done}/{Total}", i + batch.Count, items.Count);
            }
        }
    }

    public async Task DeleteAsync(IReadOnlyList<Guid> jobIds, CancellationToken ct = default)
    {
        if (jobIds.Count == 0)
            return;

        try
        {
            await client.DeleteAsync(CollectionName, jobIds, cancellationToken: ct);
            logger.LogInformation("Deleted {Count} vectors from {Collection}", jobIds.Count, CollectionName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete {Count} vectors from {Collection}", jobIds.Count, CollectionName);
            throw;
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
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to check vector existence for job {JobId}", jobId);
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

    private async Task EnsureGeoIndexAsync(CancellationToken ct)
    {
        try
        {
            await client.CreatePayloadIndexAsync(
                CollectionName,
                GeoFieldName,
                PayloadSchemaType.Geo,
                cancellationToken: ct);

            logger.LogInformation("Ensured geo payload index on {Field}", GeoFieldName);
        }
        catch (Exception ex)
        {
            // Index may already exist — this is expected
            logger.LogDebug(ex, "Geo index creation returned error (may already exist)");
        }
    }

    private static Value BuildGeoPointValue(GeoPayload geo)
    {
        return new Value
        {
            StructValue = new Struct
            {
                Fields =
                {
                    ["lon"] = new Value { DoubleValue = geo.Longitude },
                    ["lat"] = new Value { DoubleValue = geo.Latitude }
                }
            }
        };
    }
}
