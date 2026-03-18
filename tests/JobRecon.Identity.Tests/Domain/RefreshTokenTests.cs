using FluentAssertions;
using JobRecon.Identity.Domain;
using Xunit;

namespace JobRecon.Identity.Tests.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void IsExpired_WhenExpiresAtIsInFuture_ReturnsFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        // Act & Assert
        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsInPast_ReturnsTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        // Act & Assert
        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsNow_ReturnsTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        // Act & Assert
        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenNotRevokedAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        // Act & Assert
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenRevoked_ReturnsFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = true
        };

        // Act & Assert
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            IsRevoked = false
        };

        // Act & Assert
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenBothRevokedAndExpired_ReturnsFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            IsRevoked = true
        };

        // Act & Assert
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_SetsIsRevokedToTrue()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        // Act
        token.Revoke();

        // Assert
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Revoke_SetsRevokedAtToNow()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        var before = DateTime.UtcNow;

        // Act
        token.Revoke();

        // Assert
        token.RevokedAt.Should().NotBeNull();
        token.RevokedAt.Should().BeCloseTo(before, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Revoke_WithReplacementHash_SetsReplacedByTokenHash()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        var replacementHash = "new-token-hash";

        // Act
        token.Revoke(replacementHash);

        // Assert
        token.ReplacedByTokenHash.Should().Be(replacementHash);
    }

    [Fact]
    public void Revoke_WithoutReplacementHash_LeavesReplacedByTokenHashNull()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        // Act
        token.Revoke();

        // Assert
        token.ReplacedByTokenHash.Should().BeNull();
    }

    [Fact]
    public void CreatedAt_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Assert
        token.CreatedAt.Should().BeCloseTo(before, TimeSpan.FromSeconds(1));
    }
}
