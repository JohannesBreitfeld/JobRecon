namespace JobRecon.Matching.Configuration;

public sealed class GrpcServiceAddresses
{
    public const string SectionName = "GrpcServices";

    public string ProfileService { get; set; } = "http://localhost:5012";
    public string JobsService { get; set; } = "http://localhost:5013";
}
