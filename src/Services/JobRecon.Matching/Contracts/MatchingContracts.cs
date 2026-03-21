namespace JobRecon.Matching.Contracts;

// Requests
public sealed record GetRecommendationsRequest(
    int PageSize = 20,
    int Page = 1,
    double MinScore = 0.0);

// Responses
public sealed record JobRecommendation(
    Guid JobId,
    string Title,
    string CompanyName,
    string? CompanyLogoUrl,
    string? Location,
    string? WorkLocationType,
    string? EmploymentType,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    DateTime? PostedAt,
    string? ExternalUrl,
    double MatchScore,
    List<MatchFactor> MatchFactors);

public sealed record MatchFactor(
    string Category,
    string Description,
    double Score,
    double Weight);

public sealed record RecommendationsResponse(
    List<JobRecommendation> Recommendations,
    int TotalCount,
    int Page,
    int PageSize,
    MatchingSummary Summary);

public sealed record MatchingSummary(
    int TotalJobsAnalyzed,
    int MatchedJobs,
    double AverageScore,
    List<string> TopMatchingSkills,
    List<string> TopMatchingTitles);

// DTOs for external service responses
public sealed record ProfileDto(
    Guid UserId,
    string? CurrentJobTitle,
    string? Summary,
    string? Location,
    int? YearsOfExperience,
    List<SkillDto> Skills,
    List<DesiredJobTitleDto> DesiredJobTitles,
    JobPreferenceDto? Preferences);

public sealed record SkillDto(
    string Name,
    string Level,
    int? YearsOfExperience);

public sealed record DesiredJobTitleDto(
    string Title,
    int Priority);

public sealed record JobPreferenceDto(
    decimal? MinSalary,
    decimal? MaxSalary,
    string? PreferredLocations,
    bool IsRemotePreferred,
    bool IsHybridAccepted,
    bool IsOnSiteAccepted,
    string? PreferredEmploymentTypes,
    string? PreferredIndustries,
    string? ExcludedCompanies,
    bool IsActivelyLooking);

public sealed record JobDto(
    Guid Id,
    string Title,
    string? Description,
    string? Location,
    string? WorkLocationType,
    string? EmploymentType,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    string? RequiredSkills,
    int? ExperienceYearsMin,
    int? ExperienceYearsMax,
    DateTime? PostedAt,
    string? ExternalUrl,
    CompanyDto Company,
    List<string> Tags);

public sealed record CompanyDto(
    Guid Id,
    string Name,
    string? LogoUrl,
    string? Industry);

public sealed record JobListDto(
    List<JobDto> Jobs,
    int TotalCount);
