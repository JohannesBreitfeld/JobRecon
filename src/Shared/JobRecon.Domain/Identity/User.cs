using JobRecon.Domain.Common;
using JobRecon.Domain.Identity.Events;

namespace JobRecon.Domain.Identity;

public sealed class User : AggregateRoot<Guid>
{
    private readonly List<RefreshToken> _refreshTokens = [];
    private readonly List<ExternalLogin> _externalLogins = [];

    private User() { } // EF Core

    private User(
        Guid id,
        string email,
        string? firstName,
        string? lastName)
    {
        Id = id;
        Email = email;
        NormalizedEmail = email.ToUpperInvariant();
        FirstName = firstName;
        LastName = lastName;
        EmailConfirmed = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyList<ExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();

    public static User Create(
        string email,
        string? firstName = null,
        string? lastName = null)
    {
        var user = new User(Guid.NewGuid(), email, firstName, lastName);
        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email));
        return user;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new EmailConfirmedEvent(Id, Email));
    }

    public void UpdateProfile(string? firstName, string? lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserLoggedInEvent(Id));
    }

    public RefreshToken AddRefreshToken(string tokenHash, DateTime expiresAt, string? deviceInfo = null)
    {
        var token = RefreshToken.Create(Id, tokenHash, expiresAt, deviceInfo);
        _refreshTokens.Add(token);
        return token;
    }

    public void RevokeRefreshToken(Guid tokenId)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.Id == tokenId);
        token?.Revoke();
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(t => !t.IsRevoked))
        {
            token.Revoke();
        }
    }

    public ExternalLogin AddExternalLogin(string provider, string providerKey, string? displayName = null)
    {
        var existing = _externalLogins.FirstOrDefault(e =>
            e.Provider == provider && e.ProviderKey == providerKey);

        if (existing is not null)
            return existing;

        var login = ExternalLogin.Create(Id, provider, providerKey, displayName);
        _externalLogins.Add(login);
        return login;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RevokeAllRefreshTokens();
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
