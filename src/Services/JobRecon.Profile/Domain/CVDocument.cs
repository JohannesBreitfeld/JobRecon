namespace JobRecon.Profile.Domain;

public sealed class CVDocument
{
    public Guid Id { get; set; }
    public Guid UserProfileId { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public string? ParsedContent { get; set; }
    public bool IsParsed { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public UserProfile UserProfile { get; set; } = null!;
}
