namespace JobRecon.Matching.Clients;

public sealed record GeoPayload(double Latitude, double Longitude);

public sealed record GeoCircle(double Latitude, double Longitude, double RadiusKm);

public interface IVectorStore
{
    Task EnsureCollectionAsync(CancellationToken ct = default);
    Task UpsertAsync(Guid jobId, float[] embedding, GeoPayload? geoPayload = null, CancellationToken ct = default);
    Task<List<VectorSearchResult>> SearchAsync(float[] queryVector, int limit, IReadOnlyList<GeoCircle>? geoFilter = null, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid jobId, CancellationToken ct = default);
    Task<HashSet<Guid>> FilterExistingAsync(IEnumerable<Guid> jobIds, CancellationToken ct = default);
    Task SetGeoPayloadBatchAsync(IReadOnlyList<(Guid JobId, GeoPayload Payload)> items, CancellationToken ct = default);
}

public sealed record VectorSearchResult(Guid JobId, float Score);
