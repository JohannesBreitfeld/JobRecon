using JobRecon.Notifications.Domain;

namespace JobRecon.Notifications.Services;

public interface IDigestService
{
    Task QueueForDigestAsync(
        Guid userId,
        Guid jobId,
        string jobTitle,
        string companyName,
        double matchScore,
        string? location = null,
        string? topMatchFactors = null,
        string? jobUrl = null,
        CancellationToken ct = default);

    Task ProcessPendingDigestsAsync(CancellationToken ct = default);

    Task CleanupOldNotificationsAsync(int daysToKeep = 30, CancellationToken ct = default);
}
