using System.ComponentModel.DataAnnotations;
using JobRecon.Profile.Domain;

namespace JobRecon.Profile.Contracts;

public sealed record CreateProfileRequest
{
    public string? CurrentJobTitle { get; init; }
    public string? Summary { get; init; }
    public string? Location { get; init; }
    public string? PhoneNumber { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? GitHubUrl { get; init; }
    public string? PortfolioUrl { get; init; }
    public int? YearsOfExperience { get; init; }
    public IReadOnlyList<string>? DesiredJobTitles { get; init; }
}

public sealed record UpdateProfileRequest
{
    public string? CurrentJobTitle { get; init; }
    public string? Summary { get; init; }
    public string? Location { get; init; }
    public string? PhoneNumber { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? GitHubUrl { get; init; }
    public string? PortfolioUrl { get; init; }
    public int? YearsOfExperience { get; init; }
    public IReadOnlyList<string>? DesiredJobTitles { get; init; }
}

public sealed record ProfileResponse
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public string? CurrentJobTitle { get; init; }
    public string? Summary { get; init; }
    public string? Location { get; init; }
    public string? PhoneNumber { get; init; }
    public string? LinkedInUrl { get; init; }
    public string? GitHubUrl { get; init; }
    public string? PortfolioUrl { get; init; }
    public int? YearsOfExperience { get; init; }
    public required IReadOnlyList<string> DesiredJobTitles { get; init; }
    public required IReadOnlyList<SkillResponse> Skills { get; init; }
    public JobPreferenceResponse? JobPreference { get; init; }
    public required IReadOnlyList<CVDocumentResponse> CVDocuments { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

public sealed record AddSkillRequest
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; init; }

    [Required]
    public required SkillLevel Level { get; init; }

    public int? YearsOfExperience { get; init; }
}

public sealed record SkillResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required SkillLevel Level { get; init; }
    public int? YearsOfExperience { get; init; }
}

public sealed record UpdateJobPreferenceRequest
{
    public int? MinSalary { get; init; }
    public int? MaxSalary { get; init; }
    public string? PreferredLocations { get; init; }
    public bool IsRemotePreferred { get; init; }
    public bool IsHybridAccepted { get; init; }
    public bool IsOnSiteAccepted { get; init; }
    public EmploymentType PreferredEmploymentTypes { get; init; }
    public string? PreferredIndustries { get; init; }
    public string? ExcludedCompanies { get; init; }
    public bool IsActivelyLooking { get; init; } = true;
    public DateTime? AvailableFrom { get; init; }
    public int? NoticePeriodDays { get; init; }
}

public sealed record JobPreferenceResponse
{
    public required Guid Id { get; init; }
    public int? MinSalary { get; init; }
    public int? MaxSalary { get; init; }
    public string? PreferredLocations { get; init; }
    public required bool IsRemotePreferred { get; init; }
    public required bool IsHybridAccepted { get; init; }
    public required bool IsOnSiteAccepted { get; init; }
    public required EmploymentType PreferredEmploymentTypes { get; init; }
    public string? PreferredIndustries { get; init; }
    public string? ExcludedCompanies { get; init; }
    public required bool IsActivelyLooking { get; init; }
    public DateTime? AvailableFrom { get; init; }
    public int? NoticePeriodDays { get; init; }
}

public sealed record CVDocumentResponse
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSize { get; init; }
    public required bool IsPrimary { get; init; }
    public required bool IsParsed { get; init; }
    public required DateTime UploadedAt { get; init; }
}
