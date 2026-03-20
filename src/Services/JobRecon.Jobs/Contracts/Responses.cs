using JobRecon.Jobs.Domain;

namespace JobRecon.Jobs.Contracts;

public sealed class JobResponse
{
    public Guid Id { get; set; }
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
    public DateTime? PostedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public JobStatus Status { get; set; }
    public CompanyResponse Company { get; set; } = null!;
    public string SourceName { get; set; } = null!;
    public List<string> Tags { get; set; } = [];
    public bool IsSaved { get; set; }
    public SavedJobStatus? SavedStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class JobListResponse
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Location { get; set; }
    public WorkLocationType? WorkLocationType { get; set; }
    public EmploymentType? EmploymentType { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? SalaryCurrency { get; set; }
    public DateTime? PostedAt { get; set; }
    public string CompanyName { get; set; } = null!;
    public string? CompanyLogoUrl { get; set; }
    public bool IsSaved { get; set; }
    public SavedJobStatus? SavedStatus { get; set; }
}

public sealed class CompanyResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? Location { get; set; }
    public int? EmployeeCount { get; set; }
    public int JobCount { get; set; }
}

public sealed class JobSourceResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public JobSourceType Type { get; set; }
    public bool IsEnabled { get; set; }
    public int FetchIntervalMinutes { get; set; }
    public DateTime? LastFetchedAt { get; set; }
    public int? LastFetchJobCount { get; set; }
    public string? LastFetchError { get; set; }
}

public sealed class SavedJobResponse
{
    public Guid Id { get; set; }
    public SavedJobStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? AppliedAt { get; set; }
    public DateTime SavedAt { get; set; }
    public JobListResponse Job { get; set; } = null!;
}

public sealed class JobSearchResponse
{
    public List<JobListResponse> Jobs { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public sealed class JobStatisticsResponse
{
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int NewJobsToday { get; set; }
    public int NewJobsThisWeek { get; set; }
    public int SavedJobsCount { get; set; }
    public Dictionary<string, int> JobsBySource { get; set; } = [];
    public Dictionary<string, int> JobsByLocation { get; set; } = [];
    public Dictionary<string, int> JobsByType { get; set; } = [];
}
