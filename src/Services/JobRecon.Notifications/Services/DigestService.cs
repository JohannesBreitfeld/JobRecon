using System.Text.Json;
using JobRecon.Notifications.Contracts;
using JobRecon.Notifications.Domain;
using JobRecon.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Notifications.Services;

public sealed class DigestService : IDigestService
{
    private const int MaxParallelDigests = 5;

    private readonly NotificationsDbContext _dbContext;
    private readonly IPreferenceService _preferenceService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DigestService> _logger;

    public DigestService(
        NotificationsDbContext dbContext,
        IPreferenceService preferenceService,
        IServiceScopeFactory scopeFactory,
        ILogger<DigestService> logger)
    {
        _dbContext = dbContext;
        _preferenceService = preferenceService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task QueueForDigestAsync(
        Guid userId,
        Guid jobId,
        string jobTitle,
        string companyName,
        double matchScore,
        string? location = null,
        string? topMatchFactors = null,
        string? jobUrl = null,
        CancellationToken ct = default)
    {
        var item = DigestQueueItem.Create(
            userId, jobId, jobTitle, companyName, matchScore,
            location, topMatchFactors, jobUrl);

        _dbContext.DigestQueue.Add(item);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogDebug(
            "Queued job {JobId} for digest for user {UserId}",
            jobId, userId);
    }

    public async Task ProcessPendingDigestsAsync(CancellationToken ct = default)
    {
        var currentTime = TimeOnly.FromDateTime(DateTime.UtcNow);

        // Process daily digests
        await ProcessDigestsForFrequencyAsync(DigestFrequency.Daily, currentTime, ct);

        // Process weekly digests on Mondays
        if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Monday)
        {
            await ProcessDigestsForFrequencyAsync(DigestFrequency.Weekly, currentTime, ct);
        }
    }

    private async Task ProcessDigestsForFrequencyAsync(
        DigestFrequency frequency,
        TimeOnly currentTime,
        CancellationToken ct)
    {
        var usersReadyForDigest = await _preferenceService
            .GetUsersReadyForDigestAsync(frequency, currentTime, ct);

        _logger.LogInformation(
            "Processing {Frequency} digests for {Count} users",
            frequency, usersReadyForDigest.Count);

        await Parallel.ForEachAsync(
            usersReadyForDigest,
            new ParallelOptions { MaxDegreeOfParallelism = MaxParallelDigests, CancellationToken = ct },
            async (preference, token) =>
            {
                try
                {
                    await ProcessUserDigestAsync(preference, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to process digest for user {UserId}",
                        preference.UserId);
                }
            });
    }

    private async Task ProcessUserDigestAsync(
        NotificationPreference preference,
        CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var profileClient = scope.ServiceProvider.GetRequiredService<IProfileClient>();

        var pendingItems = await dbContext.DigestQueue
            .Where(d => d.UserId == preference.UserId && !d.IsProcessed)
            .OrderByDescending(d => d.MatchScore)
            .Take(50)
            .ToListAsync(ct);

        if (pendingItems.Count == 0)
        {
            return;
        }

        var userId = preference.UserId;
        var email = preference.OverrideEmail;

        if (string.IsNullOrEmpty(email))
        {
            var userEmail = await profileClient.GetUserEmailAsync(userId, ct);
            email = userEmail?.Email;
        }

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("No email found for user {UserId}, skipping digest", userId);
            return;
        }

        // Create digest DTO
        var digestItems = pendingItems.Select(item => new DigestItemDto(
            item.JobId,
            item.JobTitle,
            item.CompanyName,
            item.Location,
            item.MatchScore,
            item.TopMatchFactors,
            item.JobUrl)).ToList();

        var periodStart = pendingItems.Min(i => i.QueuedAt);
        var periodEnd = pendingItems.Max(i => i.QueuedAt);

        var digest = new DigestEmailDto(digestItems, pendingItems.Count, periodStart, periodEnd);

        // Send email
        var emailSent = await emailService.SendDigestEmailAsync(email, null, digest, preference.UnsubscribeToken, ct);

        if (emailSent)
        {
            // Mark items as processed
            foreach (var item in pendingItems)
            {
                item.MarkAsProcessed();
            }
            await dbContext.SaveChangesAsync(ct);

            // Create in-app notification for digest
            if (preference.InAppEnabled)
            {
                await notificationService.CreateNotificationAsync(
                    userId,
                    NotificationType.DigestSummary,
                    NotificationChannel.InApp,
                    $"Daily Digest: {pendingItems.Count} job matches",
                    $"You have {pendingItems.Count} new job matches. Check your email for details.",
                    JsonSerializer.Serialize(new { JobCount = pendingItems.Count }),
                    ct: ct);
            }

            _logger.LogInformation(
                "Sent digest with {Count} jobs to user {UserId}",
                pendingItems.Count, userId);
        }
    }

    public async Task CleanupOldNotificationsAsync(int daysToKeep = 30, CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

        // Delete old read notifications
        var deletedNotifications = await _dbContext.Notifications
            .Where(n => n.IsRead && n.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync(ct);

        // Delete old processed digest items
        var deletedDigestItems = await _dbContext.DigestQueue
            .Where(d => d.IsProcessed && d.ProcessedAt < cutoffDate)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation(
            "Cleanup completed: deleted {Notifications} notifications and {DigestItems} digest items",
            deletedNotifications, deletedDigestItems);
    }
}
