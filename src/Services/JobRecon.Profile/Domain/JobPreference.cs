namespace JobRecon.Profile.Domain;

public sealed class JobPreference
{
    public Guid Id { get; set; }
    public Guid UserProfileId { get; set; }
    public int? MinSalary { get; set; }
    public int? MaxSalary { get; set; }
    public ICollection<PreferredLocation> PreferredLocations { get; set; } = [];
    public bool IsRemotePreferred { get; set; }
    public bool IsHybridAccepted { get; set; }
    public bool IsOnSiteAccepted { get; set; }
    public EmploymentType PreferredEmploymentTypes { get; set; }
    public string? PreferredIndustries { get; set; }
    public string? ExcludedCompanies { get; set; }
    public bool IsActivelyLooking { get; set; } = true;
    public DateTime? AvailableFrom { get; set; }
    public int? NoticePeriodDays { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UserProfile UserProfile { get; set; } = null!;
}

[Flags]
public enum EmploymentType
{
    None = 0,
    FullTime = 1,
    PartTime = 2,
    Contract = 4,
    Freelance = 8,
    Internship = 16
}
