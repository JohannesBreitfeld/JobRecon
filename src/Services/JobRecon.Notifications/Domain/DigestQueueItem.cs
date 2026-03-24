namespace JobRecon.Notifications.Domain;

public sealed class DigestQueueItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid JobId { get; set; }
    public required string JobTitle { get; set; }
    public required string CompanyName { get; set; }
    public string? Location { get; set; }
    public double MatchScore { get; set; }
    public string? TopMatchFactors { get; set; }
    public string? JobUrl { get; set; }
    public DateTime QueuedAt { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public static DigestQueueItem Create(
        Guid userId,
        Guid jobId,
        string jobTitle,
        string companyName,
        double matchScore,
        string? location = null,
        string? topMatchFactors = null,
        string? jobUrl = null)
    {
        return new DigestQueueItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JobId = jobId,
            JobTitle = jobTitle,
            CompanyName = companyName,
            Location = location,
            MatchScore = matchScore,
            TopMatchFactors = topMatchFactors,
            JobUrl = jobUrl,
            QueuedAt = DateTime.UtcNow,
            IsProcessed = false
        };
    }

    public void MarkAsProcessed()
    {
        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
    }
}
