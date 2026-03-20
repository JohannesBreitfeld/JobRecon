namespace JobRecon.Jobs.Contracts;

/// <summary>
/// Service for enriching job ads by crawling the original job posting URLs
/// to get complete descriptions and additional details.
/// </summary>
public interface IJobEnrichmentService
{
    /// <summary>
    /// Enrich a single job by fetching data from its external URL.
    /// </summary>
    Task EnrichJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enrich multiple pending jobs in batches.
    /// </summary>
    /// <param name="batchSize">Number of jobs to process in this batch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of jobs successfully enriched</returns>
    Task<int> EnrichPendingJobsAsync(int batchSize = 50, CancellationToken cancellationToken = default);
}
