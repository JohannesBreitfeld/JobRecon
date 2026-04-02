namespace JobRecon.Identity.Configuration;

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public List<RateLimitRule> Rules { get; set; } = [];
}

public sealed class RateLimitRule
{
    public required string Endpoint { get; set; }
    public int PeriodSeconds { get; set; } = 900;
    public int Limit { get; set; } = 5;
}
