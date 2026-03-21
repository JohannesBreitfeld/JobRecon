namespace JobRecon.Notifications.Domain;

public enum NotificationType
{
    NewMatch,
    DigestSummary,
    SystemAlert
}

public enum NotificationChannel
{
    Email,
    InApp
}

public enum DigestFrequency
{
    Daily,
    Weekly,
    Never
}
