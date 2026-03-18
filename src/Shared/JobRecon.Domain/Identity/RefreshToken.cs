using JobRecon.Domain.Common;

namespace JobRecon.Domain.Identity;

public sealed class RefreshToken : Entity<Guid>
{
    private RefreshToken() { } // EF Core

    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        string? deviceInfo)
    {
        Id = id;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        DeviceInfo = deviceInfo;
        CreatedAt = DateTime.UtcNow;
        IsRevoked = false;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    internal static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        string? deviceInfo = null)
    {
        return new RefreshToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            expiresAt,
            deviceInfo);
    }

    public void Revoke(string? replacedByTokenHash = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
