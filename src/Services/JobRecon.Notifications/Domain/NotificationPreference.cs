namespace JobRecon.Notifications.Domain;

public class NotificationPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public bool DigestEnabled { get; set; } = true;
    public DigestFrequency DigestFrequency { get; set; } = DigestFrequency.Daily;
    public TimeOnly DigestTime { get; set; } = new(8, 0);
    public double MinMatchScoreForRealtime { get; set; } = 0.8;
    public string? OverrideEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static NotificationPreference CreateDefault(Guid userId)
    {
        var now = DateTime.UtcNow;
        return new NotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmailEnabled = true,
            InAppEnabled = true,
            DigestEnabled = true,
            DigestFrequency = DigestFrequency.Daily,
            DigestTime = new TimeOnly(8, 0),
            MinMatchScoreForRealtime = 0.8,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        bool? emailEnabled = null,
        bool? inAppEnabled = null,
        bool? digestEnabled = null,
        DigestFrequency? digestFrequency = null,
        TimeOnly? digestTime = null,
        double? minMatchScoreForRealtime = null,
        string? overrideEmail = null)
    {
        if (emailEnabled.HasValue) EmailEnabled = emailEnabled.Value;
        if (inAppEnabled.HasValue) InAppEnabled = inAppEnabled.Value;
        if (digestEnabled.HasValue) DigestEnabled = digestEnabled.Value;
        if (digestFrequency.HasValue) DigestFrequency = digestFrequency.Value;
        if (digestTime.HasValue) DigestTime = digestTime.Value;
        if (minMatchScoreForRealtime.HasValue) MinMatchScoreForRealtime = minMatchScoreForRealtime.Value;
        if (overrideEmail is not null) OverrideEmail = overrideEmail;

        UpdatedAt = DateTime.UtcNow;
    }
}
