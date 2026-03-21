namespace JobRecon.Notifications.Domain;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public string? Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? SentAt { get; set; }
    public Guid? EventId { get; set; }

    public static Notification Create(
        Guid userId,
        NotificationType type,
        NotificationChannel channel,
        string title,
        string body,
        string? data = null,
        Guid? eventId = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Channel = channel,
            Title = title,
            Body = body,
            Data = data,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            EventId = eventId
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }

    public void MarkAsSent()
    {
        SentAt = DateTime.UtcNow;
    }
}
