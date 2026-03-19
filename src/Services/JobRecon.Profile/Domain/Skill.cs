namespace JobRecon.Profile.Domain;

public sealed class Skill
{
    public Guid Id { get; set; }
    public Guid UserProfileId { get; set; }
    public string Name { get; set; } = null!;
    public SkillLevel Level { get; set; }
    public int? YearsOfExperience { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserProfile UserProfile { get; set; } = null!;
}

public enum SkillLevel
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3,
    Expert = 4
}
