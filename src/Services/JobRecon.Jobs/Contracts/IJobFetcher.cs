using JobRecon.Jobs.Domain;

namespace JobRecon.Jobs.Contracts;

public interface IJobFetcher
{
    JobSourceType SourceType { get; }
    IAsyncEnumerable<FetchedJobBatch> FetchJobBatchesAsync(JobSource source, CancellationToken cancellationToken = default);
}

/// <summary>
/// A batch of fetched jobs from a single logical unit (e.g., one date file).
/// The service persists jobs and checkpoint data after each batch.
/// </summary>
public sealed class FetchedJobBatch
{
    public required List<FetchedJob> Jobs { get; init; }
    public required string? CheckpointConfig { get; init; }
}

public sealed class FetchedJob
{
    public required string ExternalId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string CompanyName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public string? CompanyWebsite { get; set; }
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
    public List<string> Tags { get; set; } = [];
}
