namespace JobRecon.Profile.Domain;

public sealed class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? CurrentJobTitle { get; set; }
    public string? Summary { get; set; }
    public string? Location { get; set; }
    public string? PhoneNumber { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public int? YearsOfExperience { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DesiredJobTitle> DesiredJobTitles { get; set; } = [];
    public ICollection<Skill> Skills { get; set; } = [];
    public JobPreference? JobPreference { get; set; }
    public ICollection<CVDocument> CVDocuments { get; set; } = [];
}
