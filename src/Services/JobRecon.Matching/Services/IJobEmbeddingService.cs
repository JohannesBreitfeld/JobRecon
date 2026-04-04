namespace JobRecon.Matching.Services;

public interface IJobEmbeddingService
{
    Task<int> EmbedPendingJobsAsync(CancellationToken ct = default);
    Task<int> BackfillGeoPayloadAsync(CancellationToken ct = default);
}
