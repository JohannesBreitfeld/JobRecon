using JobRecon.Matching.Services;

namespace JobRecon.Matching.Workers;

public sealed class JobEmbeddingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<JobEmbeddingWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        logger.LogInformation("Job embedding worker started");

        // One-time geo payload backfill for existing embeddings
        await RunGeoBackfillAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var embeddingService = scope.ServiceProvider.GetRequiredService<IJobEmbeddingService>();
                await embeddingService.EmbedPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error in job embedding worker");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunGeoBackfillAsync(CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Starting geo payload backfill for existing embeddings");

            using var scope = scopeFactory.CreateScope();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IJobEmbeddingService>();
            var count = await embeddingService.BackfillGeoPayloadAsync(ct);

            logger.LogInformation("Geo payload backfill complete: {Count} points updated", count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Geo payload backfill failed, will retry on next restart");
        }
    }
}
