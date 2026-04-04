using JobRecon.Matching.Clients;
using JobRecon.Matching.Contracts;

namespace JobRecon.Matching.Services;

public sealed class JobEmbeddingService(
    IJobsClient jobsClient,
    IOllamaClient ollamaClient,
    IVectorStore vectorStore,
    ILogger<JobEmbeddingService> logger) : IJobEmbeddingService
{
    private const int FetchBatchSize = 100;
    private const int MaxJobsPerCycle = 100_000;
    private const int MaxConcurrentEmbeddings = 4;

    public async Task<int> EmbedPendingJobsAsync(CancellationToken ct = default)
    {
        await vectorStore.EnsureCollectionAsync(ct);

        var offset = 0;
        var embedded = 0;
        using var semaphore = new SemaphoreSlim(MaxConcurrentEmbeddings);

        while (offset < MaxJobsPerCycle)
        {
            var jobsResponse = await jobsClient.GetActiveJobsAsync(FetchBatchSize, offset, ct);
            if (jobsResponse is null || jobsResponse.Jobs.Count == 0)
                break;

            var jobIds = jobsResponse.Jobs.Select(j => j.Id);
            var existingIds = await vectorStore.FilterExistingAsync(jobIds, ct);
            var newJobs = jobsResponse.Jobs.Where(j => !existingIds.Contains(j.Id)).ToList();

            if (newJobs.Count > 0)
            {
                var tasks = newJobs.Select(async job =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        var text = BuildJobText(job);
                        var embedding = await ollamaClient.GetEmbeddingAsync(text, ct);
                        if (embedding is null)
                            return false;

                        var geoPayload = (job.Latitude.HasValue && job.Longitude.HasValue)
                            ? new GeoPayload(job.Latitude.Value, job.Longitude.Value)
                            : null;

                        await vectorStore.UpsertAsync(job.Id, embedding, geoPayload, ct);
                        return true;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var results = await Task.WhenAll(tasks);
                embedded += results.Count(r => r);
            }

            offset += FetchBatchSize;

            if (jobsResponse.Jobs.Count < FetchBatchSize)
                break;
        }

        if (embedded > 0)
        {
            logger.LogInformation("Embedded {Count} jobs into vector store", embedded);
        }

        return embedded;
    }

    public async Task<int> BackfillGeoPayloadAsync(CancellationToken ct = default)
    {
        await vectorStore.EnsureCollectionAsync(ct);

        var offset = 0;
        var updated = 0;

        while (offset < MaxJobsPerCycle)
        {
            var jobsResponse = await jobsClient.GetActiveJobsAsync(FetchBatchSize, offset, ct);
            if (jobsResponse is null || jobsResponse.Jobs.Count == 0)
                break;

            // Only consider jobs that have coordinates
            var geocodedJobs = jobsResponse.Jobs
                .Where(j => j.Latitude.HasValue && j.Longitude.HasValue)
                .ToList();

            if (geocodedJobs.Count > 0)
            {
                // Check which exist in Qdrant
                var existingIds = await vectorStore.FilterExistingAsync(
                    geocodedJobs.Select(j => j.Id), ct);

                var toUpdate = geocodedJobs
                    .Where(j => existingIds.Contains(j.Id))
                    .Select(j => (j.Id, new GeoPayload(j.Latitude!.Value, j.Longitude!.Value)))
                    .ToList();

                if (toUpdate.Count > 0)
                {
                    await vectorStore.SetGeoPayloadBatchAsync(toUpdate, ct);
                    updated += toUpdate.Count;
                }
            }

            offset += FetchBatchSize;

            if (jobsResponse.Jobs.Count < FetchBatchSize)
                break;
        }

        if (updated > 0)
        {
            logger.LogInformation("Backfilled geo payload for {Count} existing embeddings", updated);
        }

        return updated;
    }

    internal static string BuildJobText(JobDto job)
    {
        var parts = new List<string> { job.Title };

        if (!string.IsNullOrWhiteSpace(job.Description))
            parts.Add(job.Description);

        if (!string.IsNullOrWhiteSpace(job.RequiredSkills))
            parts.Add($"Skills: {job.RequiredSkills}");

        if (!string.IsNullOrWhiteSpace(job.Location))
            parts.Add($"Location: {job.Location}");

        if (!string.IsNullOrWhiteSpace(job.EmploymentType))
            parts.Add(job.EmploymentType);

        if (job.Tags.Count > 0)
            parts.Add($"Tags: {string.Join(", ", job.Tags)}");

        parts.Add($"Company: {job.Company.Name}");

        if (!string.IsNullOrWhiteSpace(job.Company.Industry))
            parts.Add($"Industry: {job.Company.Industry}");

        return string.Join(". ", parts);
    }
}
