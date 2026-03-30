namespace JobRecon.Matching.Services;

public interface IJobEmbeddingService
{
    Task<int> EmbedPendingJobsAsync(CancellationToken ct = default);
}
