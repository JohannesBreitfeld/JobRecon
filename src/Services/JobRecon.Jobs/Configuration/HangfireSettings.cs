namespace JobRecon.Jobs.Configuration;

public sealed class HangfireSettings
{
    public const string SectionName = "Hangfire";

    public string ConnectionString { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "hangfire";
    public int WorkerCount { get; set; } = 2;
}
