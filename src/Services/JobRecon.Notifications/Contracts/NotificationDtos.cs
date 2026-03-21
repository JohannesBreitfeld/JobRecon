using JobRecon.Notifications.Domain;

namespace JobRecon.Notifications.Contracts;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    NotificationChannel Channel,
    string Title,
    string Body,
    JobMatchData? JobMatch,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

public record JobMatchData(
    Guid JobId,
    string JobTitle,
    string CompanyName,
    string? Location,
    double MatchScore,
    List<MatchFactorData>? TopFactors,
    string? JobUrl);

public record MatchFactorData(
    string Category,
    double Score,
    string? Description);

public record CreateNotificationRequest(
    NotificationType Type,
    NotificationChannel Channel,
    string Title,
    string Body,
    string? Data = null);

public record NotificationsResponse(
    List<NotificationDto> Notifications,
    int TotalCount,
    int Page,
    int PageSize);

public record UnreadCountResponse(int UnreadCount);
