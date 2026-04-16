using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Jobs.Services;

public sealed class JobExpirationService(
    JobsDbContext dbContext,
    ILogger<JobExpirationService> logger) : IJobExpirationService
{
    public async Task<int> ExpireJobsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var expiredCount = await dbContext.Jobs
            .Where(j => j.Status == JobStatus.Active &&
                        j.ExpiresAt != null &&
                        j.ExpiresAt < now)
            .ExecuteUpdateAsync(
                s => s.SetProperty(j => j.Status, JobStatus.Expired)
                      .SetProperty(j => j.UpdatedAt, now),
                cancellationToken);

        if (expiredCount > 0)
            logger.LogInformation("Marked {Count} jobs as expired", expiredCount);
        else
            logger.LogDebug("No jobs to expire");

        return expiredCount;
    }
}
