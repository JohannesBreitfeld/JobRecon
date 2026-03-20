namespace JobRecon.Jobs.Domain;

public sealed class JobTag
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? NormalizedName { get; set; }

    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
}
