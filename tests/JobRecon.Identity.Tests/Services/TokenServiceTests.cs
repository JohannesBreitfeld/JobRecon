using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using JobRecon.Identity.Configuration;
using JobRecon.Identity.Domain;
using JobRecon.Identity.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace JobRecon.Identity.Tests.Services;

public class TokenServiceTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SigningKey = "TestSigningKeyThatIsAtLeast32CharactersLong!",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationMinutes = 10080
        };

        var options = Options.Create(_jwtSettings);
        _sut = new TokenService(options);
    }

    [Fact]
    public void GenerateAccessToken_WithValidUser_ReturnsValidJwt()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new[] { "User", "Admin" };

        // Act
        var token = _sut.GenerateAccessToken(user, roles);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be(_jwtSettings.Issuer);
        jwtToken.Audiences.Should().Contain(_jwtSettings.Audience);
    }

    [Fact]
    public void GenerateAccessToken_IncludesUserIdClaim()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = Array.Empty<string>();

        // Act
        var token = _sut.GenerateAccessToken(user, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Subject.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void GenerateAccessToken_IncludesEmailClaim()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = Array.Empty<string>();

        // Act
        var token = _sut.GenerateAccessToken(user, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be(user.Email);
    }

    [Fact]
    public void GenerateAccessToken_IncludesRoleClaims()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = new[] { "User", "Admin" };

        // Act
        var token = _sut.GenerateAccessToken(user, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var roleClaims = jwtToken.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        roleClaims.Should().Contain("User");
        roleClaims.Should().Contain("Admin");
    }

    [Fact]
    public void GenerateAccessToken_WithFirstName_IncludesGivenNameClaim()
    {
        // Arrange
        var user = CreateTestUser();
        user.FirstName = "Test";
        var roles = Array.Empty<string>();

        // Act
        var token = _sut.GenerateAccessToken(user, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var givenNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName);
        givenNameClaim.Should().NotBeNull();
        givenNameClaim!.Value.Should().Be("Test");
    }

    [Fact]
    public void GenerateAccessToken_WithLastName_IncludesSurnameClaim()
    {
        // Arrange
        var user = CreateTestUser();
        user.LastName = "User";
        var roles = Array.Empty<string>();

        // Act
        var token = _sut.GenerateAccessToken(user, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var surnameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname);
        surnameClaim.Should().NotBeNull();
        surnameClaim!.Value.Should().Be("User");
    }

    [Fact]
    public void GenerateAccessToken_WithoutFirstName_DoesNotIncludeGivenNameClaim()
    {
        // Arrange
        var user = CreateTestUser();
        user.FirstName = null;
        var roles = Array.Empty<string>();

        // Act
        var token = _sut.GenerateAccessToken(user, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var givenNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName);
        givenNameClaim.Should().BeNull();
    }

    [Fact]
    public void GenerateAccessToken_HasCorrectExpiration()
    {
        // Arrange
        var user = CreateTestUser();
        var roles = Array.Empty<string>();
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _sut.GenerateAccessToken(user, roles);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiration = beforeGeneration.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Act
        var token = _sut.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var action = () => Convert.FromBase64String(token);
        action.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        // Act
        var tokens = Enumerable.Range(0, 100)
            .Select(_ => _sut.GenerateRefreshToken())
            .ToList();

        // Assert
        tokens.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GenerateRefreshToken_HasSufficientLength()
    {
        // Act
        var token = _sut.GenerateRefreshToken();
        var bytes = Convert.FromBase64String(token);

        // Assert - should be 64 bytes (512 bits)
        bytes.Should().HaveCount(64);
    }

    [Fact]
    public void HashToken_ReturnsDeterministicHash()
    {
        // Arrange
        var token = "test-token-12345";

        // Act
        var hash1 = _sut.HashToken(token);
        var hash2 = _sut.HashToken(token);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_ReturnsBase64String()
    {
        // Arrange
        var token = "test-token-12345";

        // Act
        var hash = _sut.HashToken(token);

        // Assert
        var action = () => Convert.FromBase64String(hash);
        action.Should().NotThrow();
    }

    [Fact]
    public void HashToken_DifferentTokensProduceDifferentHashes()
    {
        // Arrange
        var token1 = "test-token-1";
        var token2 = "test-token-2";

        // Act
        var hash1 = _sut.HashToken(token1);
        var hash2 = _sut.HashToken(token2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GetAccessTokenExpiration_ReturnsCorrectTime()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var expiration = _sut.GetAccessTokenExpiration();

        // Assert
        var expectedExpiration = before.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        expiration.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetRefreshTokenExpiration_ReturnsCorrectTime()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var expiration = _sut.GetRefreshTokenExpiration();

        // Assert
        var expectedExpiration = before.AddMinutes(_jwtSettings.RefreshTokenExpirationMinutes);
        expiration.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(1));
    }

    private static ApplicationUser CreateTestUser()
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = null,
            LastName = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }
}
