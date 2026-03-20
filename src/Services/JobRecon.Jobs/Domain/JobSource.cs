namespace JobRecon.Jobs.Domain;

public sealed class JobSource
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required JobSourceType Type { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Configuration { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int FetchIntervalMinutes { get; set; } = 60;
    public DateTime? LastFetchedAt { get; set; }
    public int? LastFetchJobCount { get; set; }
    public string? LastFetchError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Job> Jobs { get; set; } = [];
}
