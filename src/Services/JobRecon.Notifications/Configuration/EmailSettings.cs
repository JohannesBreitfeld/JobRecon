namespace JobRecon.Notifications.Configuration;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "notifications@jobrecon.com";
    public string FromName { get; set; } = "JobRecon";
    public int MaxEmailsPerHour { get; set; } = 100;
}
