namespace JobRecon.Jobs.Domain;

public sealed class SavedJob
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public SavedJobStatus Status { get; set; } = SavedJobStatus.Saved;
    public string? Notes { get; set; }
    public DateTime? AppliedAt { get; set; }
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
}
