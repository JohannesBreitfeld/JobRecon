namespace JobRecon.Profile.Domain;

public sealed class DesiredJobTitle
{
    public Guid Id { get; set; }
    public Guid UserProfileId { get; set; }
    public string Title { get; set; } = null!;
    public int Priority { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserProfile UserProfile { get; set; } = null!;
}
