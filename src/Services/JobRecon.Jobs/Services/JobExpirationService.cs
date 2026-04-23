using JobRecon.Contracts.Events;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Jobs.Services;

public sealed class JobExpirationService(
    JobsDbContext dbContext,
    IJobEventPublisher eventPublisher,
    ILogger<JobExpirationService> logger) : IJobExpirationService
{
    public async Task<int> ExpireJobsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var expiredIds = await dbContext.Jobs
            .Where(j => j.Status == JobStatus.Active &&
                        j.ExpiresAt != null &&
                        j.ExpiresAt < now)
            .Select(j => j.Id)
            .ToListAsync(cancellationToken);

        if (expiredIds.Count == 0)
        {
            logger.LogDebug("No jobs to expire");
            return 0;
        }

        var expiredCount = await dbContext.Jobs
            .Where(j => expiredIds.Contains(j.Id))
            .ExecuteUpdateAsync(
                s => s.SetProperty(j => j.Status, JobStatus.Expired)
                      .SetProperty(j => j.UpdatedAt, now),
                cancellationToken);

        logger.LogInformation("Marked {Count} jobs as expired", expiredCount);

        await eventPublisher.PublishJobsExpiredAsync(
            new JobsExpiredIntegrationEvent(Guid.NewGuid(), expiredIds, now),
            cancellationToken);

        return expiredCount;
    }
}
