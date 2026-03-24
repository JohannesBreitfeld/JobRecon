namespace JobRecon.Notifications.Configuration;

public sealed class HangfireSettings
{
    public const string SectionName = "Hangfire";

    public string? ConnectionString { get; set; }
    public int WorkerCount { get; set; } = 2;
}
