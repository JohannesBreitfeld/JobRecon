using System.Text.Json;
using JobRecon.Notifications.Contracts;
using JobRecon.Notifications.Domain;
using JobRecon.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationsDbContext dbContext,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Notification> CreateNotificationAsync(
        Guid userId,
        NotificationType type,
        NotificationChannel channel,
        string title,
        string body,
        string? data = null,
        Guid? eventId = null,
        CancellationToken ct = default)
    {
        var notification = Notification.Create(userId, type, channel, title, body, data, eventId);

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created notification {NotificationId} for user {UserId}, type: {Type}",
            notification.Id, userId, type);

        return notification;
    }

    public async Task<NotificationsResponse> GetUserNotificationsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        bool? unreadOnly = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.Notifications
            .Where(n => n.UserId == userId && n.Channel == NotificationChannel.InApp);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        var totalCount = await query.CountAsync(ct);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = notifications.Select(MapToDto).ToList();

        return new NotificationsResponse(dtos, totalCount, page, pageSize);
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (notification is null)
        {
            return false;
        }

        notification.MarkAsRead();
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var count = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, DateTime.UtcNow),
                ct);

        _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", count, userId);

        return count;
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbContext.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead && n.Channel == NotificationChannel.InApp, ct);
    }

    public async Task<bool> HasEventBeenProcessedAsync(Guid eventId, CancellationToken ct = default)
    {
        return await _dbContext.Notifications
            .AnyAsync(n => n.EventId == eventId, ct);
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        JobMatchData? jobMatch = null;

        if (notification.Data is not null && notification.Type == NotificationType.NewMatch)
        {
            try
            {
                jobMatch = JsonSerializer.Deserialize<JobMatchData>(notification.Data);
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Channel,
            notification.Title,
            notification.Body,
            jobMatch,
            notification.IsRead,
            notification.CreatedAt,
            notification.ReadAt);
    }
}
