using JobRecon.Domain.Common;

namespace JobRecon.Domain.Identity;

public sealed class ExternalLogin : Entity<Guid>
{
    private ExternalLogin() { } // EF Core

    private ExternalLogin(
        Guid id,
        Guid userId,
        string provider,
        string providerKey,
        string? displayName)
    {
        Id = id;
        UserId = userId;
        Provider = provider;
        ProviderKey = providerKey;
        DisplayName = displayName;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = null!;
    public string ProviderKey { get; private set; } = null!;
    public string? DisplayName { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal static ExternalLogin Create(
        Guid userId,
        string provider,
        string providerKey,
        string? displayName = null)
    {
        return new ExternalLogin(
            Guid.NewGuid(),
            userId,
            provider,
            providerKey,
            displayName);
    }
}

public static class ExternalLoginProviders
{
    public const string Google = "Google";
    public const string GitHub = "GitHub";
    public const string Microsoft = "Microsoft";
    public const string EntraId = "EntraId";
}
