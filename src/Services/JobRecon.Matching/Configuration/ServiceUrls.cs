namespace JobRecon.Matching.Configuration;

public sealed class ServiceUrls
{
    public const string SectionName = "ServiceUrls";

    public string ProfileService { get; set; } = "http://localhost:5002";
    public string JobsService { get; set; } = "http://localhost:5004";
}
