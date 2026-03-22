using JobRecon.Jobs.Domain;

namespace JobRecon.Jobs.Contracts;

public sealed class JobSearchRequest
{
    public string? Query { get; set; }
    public string? Location { get; set; }
    public WorkLocationType? WorkLocationType { get; set; }
    public EmploymentType? EmploymentType { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? JobSourceId { get; set; }
    public int? ExperienceYearsMax { get; set; }
    public string? Tags { get; set; }
    public bool? SavedOnly { get; set; }
    public DateTime? PostedAfter { get; set; }
    public string? SortBy { get; set; }
    public bool? SortDescending { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

public sealed class SaveJobRequest
{
    public string? Notes { get; set; }
}

public sealed class UpdateSavedJobRequest
{
    public SavedJobStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? AppliedAt { get; set; }
}

public sealed class CreateJobSourceRequest
{
    public required string Name { get; set; }
    public required JobSourceType Type { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Configuration { get; set; }
    public int FetchIntervalMinutes { get; set; } = 60;
}

public sealed class UpdateJobSourceRequest
{
    public string? Name { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Configuration { get; set; }
    public bool? IsEnabled { get; set; }
    public int? FetchIntervalMinutes { get; set; }
}

public sealed class CreateJobRequest
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public WorkLocationType? WorkLocationType { get; set; }
    public EmploymentType? EmploymentType { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? SalaryCurrency { get; set; }
    public string? SalaryPeriod { get; set; }
    public string? ExternalUrl { get; set; }
    public string? ApplicationUrl { get; set; }
    public string? RequiredSkills { get; set; }
    public string? Benefits { get; set; }
    public int? ExperienceYearsMin { get; set; }
    public int? ExperienceYearsMax { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public required string CompanyName { get; set; }
    public List<string>? Tags { get; set; }
}
