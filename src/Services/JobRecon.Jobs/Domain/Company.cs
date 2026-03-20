namespace JobRecon.Jobs.Domain;

public sealed class Company
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? NormalizedName { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? Location { get; set; }
    public int? EmployeeCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Job> Jobs { get; set; } = [];
}
