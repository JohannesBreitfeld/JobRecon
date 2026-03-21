using JobRecon.Notifications.Contracts;
using JobRecon.Notifications.Domain;

namespace JobRecon.Notifications.Services;

public interface INotificationService
{
    Task<Notification> CreateNotificationAsync(
        Guid userId,
        NotificationType type,
        NotificationChannel channel,
        string title,
        string body,
        string? data = null,
        Guid? eventId = null,
        CancellationToken ct = default);

    Task<NotificationsResponse> GetUserNotificationsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        bool? unreadOnly = null,
        CancellationToken ct = default);

    Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);

    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    Task<bool> HasEventBeenProcessedAsync(Guid eventId, CancellationToken ct = default);
}
