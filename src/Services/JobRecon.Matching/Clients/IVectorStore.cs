namespace JobRecon.Matching.Clients;

public interface IVectorStore
{
    Task EnsureCollectionAsync(CancellationToken ct = default);
    Task UpsertAsync(Guid jobId, float[] embedding, CancellationToken ct = default);
    Task<List<VectorSearchResult>> SearchAsync(float[] queryVector, int limit, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid jobId, CancellationToken ct = default);
}

public sealed record VectorSearchResult(Guid JobId, float Score);
