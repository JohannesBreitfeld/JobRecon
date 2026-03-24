using JobRecon.Matching.Clients;
using JobRecon.Matching.Contracts;

namespace JobRecon.Matching.Workers;

public sealed class JobEmbeddingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<JobEmbeddingWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
    private const int FetchBatchSize = 100;
    private const int MaxJobsPerCycle = 2000;
    private const int MaxConcurrentEmbeddings = 4;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the app to be fully started
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        logger.LogInformation("Job embedding worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EmbedPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error in job embedding worker");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task EmbedPendingJobsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var jobsClient = scope.ServiceProvider.GetRequiredService<IJobsClient>();
        var ollamaClient = scope.ServiceProvider.GetRequiredService<IOllamaClient>();
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();

        await vectorStore.EnsureCollectionAsync(ct);

        var offset = 0;
        var embedded = 0;
        using var semaphore = new SemaphoreSlim(MaxConcurrentEmbeddings);

        while (offset < MaxJobsPerCycle)
        {
            var jobsResponse = await jobsClient.GetActiveJobsAsync(FetchBatchSize, offset, ct);
            if (jobsResponse is null || jobsResponse.Jobs.Count == 0)
                break;

            // Batch check which jobs already have embeddings
            var jobIds = jobsResponse.Jobs.Select(j => j.Id);
            var existingIds = await vectorStore.FilterExistingAsync(jobIds, ct);
            var newJobs = jobsResponse.Jobs.Where(j => !existingIds.Contains(j.Id)).ToList();

            if (newJobs.Count > 0)
            {
                // Embed new jobs concurrently
                var tasks = newJobs.Select(async job =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        var text = BuildJobText(job);
                        var embedding = await ollamaClient.GetEmbeddingAsync(text, ct);
                        if (embedding is null)
                            return false;

                        await vectorStore.UpsertAsync(job.Id, embedding, ct);
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

            // Stop early if we've processed all jobs
            if (jobsResponse.Jobs.Count < FetchBatchSize)
                break;
        }

        if (embedded > 0)
        {
            logger.LogInformation("Embedded {Count} jobs into vector store", embedded);
        }
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
