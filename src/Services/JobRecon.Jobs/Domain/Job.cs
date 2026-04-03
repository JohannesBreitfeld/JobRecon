using JobRecon.Domain.Common;

namespace JobRecon.Jobs.Domain;

public sealed class Job : AggregateRoot<Guid>
{
    public required string Title { get; set; }
    public string? NormalizedTitle { get; set; }
    public string? Description { get; set; }
    public string? NormalizedDescription { get; set; }
    public string? Location { get; set; }
    public string? NormalizedLocation { get; set; }
    public WorkLocationType? WorkLocationType { get; set; }
    public EmploymentType? EmploymentType { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? SalaryCurrency { get; set; }
    public string? SalaryPeriod { get; set; }
    public string? ExternalId { get; set; }
    public string? ExternalUrl { get; set; }
    public string? ApplicationUrl { get; set; }
    public string? RequiredSkills { get; set; }
    public string? Benefits { get; set; }
    public int? ExperienceYearsMin { get; set; }
    public int? ExperienceYearsMax { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Active;
    public string? Hash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Geolocation
    public int? LocalityId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Enrichment tracking
    public bool IsEnriched { get; set; }
    public DateTime? EnrichedAt { get; set; }
    public string? EnrichmentError { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public Guid JobSourceId { get; set; }
    public JobSource JobSource { get; set; } = null!;

    public ICollection<JobTag> Tags { get; set; } = [];
    public ICollection<SavedJob> SavedJobs { get; set; } = [];
}
