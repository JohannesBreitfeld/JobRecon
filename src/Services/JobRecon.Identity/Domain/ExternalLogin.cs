namespace JobRecon.Identity.Domain;

public sealed class ExternalLogin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = null!;
    public string ProviderKey { get; set; } = null!;
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}

public static class ExternalLoginProviders
{
    public const string Google = "Google";
    public const string GitHub = "GitHub";
    public const string Microsoft = "Microsoft";
    public const string EntraId = "EntraId";
}
