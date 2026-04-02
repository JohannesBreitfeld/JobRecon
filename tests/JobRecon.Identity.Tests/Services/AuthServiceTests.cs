using FluentAssertions;
using JobRecon.Identity.Contracts;
using JobRecon.Identity.Domain;
using JobRecon.Identity.Infrastructure;
using JobRecon.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JobRecon.Identity.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AuthService> _logger;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new IdentityDbContext(options);

        // Setup mocks
        _userManager = CreateMockUserManager();
        _tokenService = Substitute.For<ITokenService>();
        _cache = Substitute.For<IDistributedCache>();
        _logger = Substitute.For<ILogger<AuthService>>();

        // Setup default token service behavior
        _tokenService.GenerateAccessToken(Arg.Any<ApplicationUser>(), Arg.Any<IEnumerable<string>>())
            .Returns("test-access-token");
        _tokenService.GenerateRefreshToken().Returns("test-refresh-token");
        _tokenService.HashToken(Arg.Any<string>()).Returns(callInfo => $"hashed-{callInfo.Arg<string>()}");
        _tokenService.GetAccessTokenExpiration().Returns(DateTime.UtcNow.AddMinutes(15));
        _tokenService.GetRefreshTokenExpiration().Returns(DateTime.UtcNow.AddDays(7));

        _sut = new AuthService(_userManager, _dbContext, _tokenService, _cache, _logger);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ReturnsSuccessWithTokens()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        _userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), "User")
            .Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(Arg.Any<ApplicationUser>())
            .Returns(new List<string> { "User" });

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.AccessToken.Should().Be("test-access-token");
        result.Value.RefreshToken.Should().Be("test-refresh-token");
        result.Value.User.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsConflictError()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Password123!"
        };

        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserName = request.Email
        };
        _userManager.FindByEmailAsync(request.Email).Returns(existingUser);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.EmailExists");
    }

    [Fact]
    public async Task RegisterAsync_WhenUserCreationFails_ReturnsValidationError()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "weak"
        };

        _userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = "Password must be at least 6 characters."
            }));

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.CreationFailed");
        result.Error.Message.Should().Contain("Password must be at least 6 characters");
    }

    [Fact]
    public async Task RegisterAsync_CreatesRefreshTokenInDatabase()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), request.Password)
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), "User")
            .Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(Arg.Any<ApplicationUser>())
            .Returns(new List<string> { "User" });

        // Act
        await _sut.RegisterAsync(request);

        // Assert
        var tokens = await _dbContext.RefreshTokens.ToListAsync();
        tokens.Should().HaveCount(1);
        tokens[0].TokenHash.Should().Be("hashed-test-refresh-token");
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new LoginRequest
        {
            Email = user.Email!,
            Password = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(user);
        _userManager.CheckPasswordAsync(user, request.Password).Returns(true);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "User" });
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ReturnsUnauthorizedError()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns((ApplicationUser?)null);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsUnauthorizedError()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new LoginRequest
        {
            Email = user.Email!,
            Password = "WrongPassword123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(user);
        _userManager.CheckPasswordAsync(user, request.Password).Returns(false);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task LoginAsync_WithDeactivatedAccount_ReturnsUnauthorizedError()
    {
        // Arrange
        var user = CreateTestUser();
        user.IsActive = false;
        var request = new LoginRequest
        {
            Email = user.Email!,
            Password = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(user);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.AccountDeactivated");
    }

    [Fact]
    public async Task LoginAsync_UpdatesLastLoginAt()
    {
        // Arrange
        var user = CreateTestUser();
        user.LastLoginAt = null;
        var request = new LoginRequest
        {
            Email = user.Email!,
            Password = "Password123!"
        };

        _userManager.FindByEmailAsync(request.Email).Returns(user);
        _userManager.CheckPasswordAsync(user, request.Password).Returns(true);
        _userManager.GetRolesAsync(user).Returns(new List<string> { "User" });
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        // Act
        await _sut.LoginAsync(request);

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hashed-valid-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            User = user
        };
        _dbContext.RefreshTokens.Add(existingToken);
        await _dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "valid-refresh-token" };
        _userManager.GetRolesAsync(user).Returns(new List<string> { "User" });

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ReturnsUnauthorizedError()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "invalid-token" };

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Token.Invalid");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ReturnsUnauthorizedError()
    {
        // Arrange
        var user = CreateTestUser();
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hashed-expired-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            User = user
        };
        _dbContext.RefreshTokens.Add(expiredToken);
        await _dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "expired-token" };

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Token.Invalid");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ReturnsUnauthorizedError()
    {
        // Arrange
        var user = CreateTestUser();
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hashed-revoked-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddHours(-1),
            User = user
        };
        _dbContext.RefreshTokens.Add(revokedToken);
        await _dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "revoked-token" };

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Token.Invalid");
    }

    [Fact]
    public async Task RefreshTokenAsync_RotatesToken()
    {
        // Arrange
        var user = CreateTestUser();
        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hashed-old-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            User = user
        };
        _dbContext.RefreshTokens.Add(existingToken);
        await _dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "old-token" };
        _userManager.GetRolesAsync(user).Returns(new List<string> { "User" });

        // Act
        await _sut.RefreshTokenAsync(request);

        // Assert
        var tokens = await _dbContext.RefreshTokens.ToListAsync();
        tokens.Should().HaveCount(2);

        var oldToken = tokens.First(t => t.TokenHash == "hashed-old-token");
        oldToken.IsRevoked.Should().BeTrue();
        oldToken.ReplacedByTokenHash.Should().NotBeNull();
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_WithValidToken_RevokesToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hashed-logout-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.LogoutAsync(userId, "logout-token");

        // Assert
        result.IsSuccess.Should().BeTrue();

        var revokedToken = await _dbContext.RefreshTokens.FirstAsync();
        revokedToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_WithoutToken_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _sut.LogoutAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region RevokeAllTokensAsync Tests

    [Fact]
    public async Task RevokeAllTokensAsync_RevokesAllActiveTokensForUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tokens = new[]
        {
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = "hash1",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = "hash2",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = "hash3",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = true // Already revoked
            }
        };
        _dbContext.RefreshTokens.AddRange(tokens);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.RevokeAllTokensAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var allTokens = await _dbContext.RefreshTokens.ToListAsync();
        allTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
    }

    #endregion

    private static ApplicationUser CreateTestUser()
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true,
            EmailConfirmed = true
        };
    }

    private static UserManager<ApplicationUser> CreateMockUserManager()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        var userManager = Substitute.For<UserManager<ApplicationUser>>(
            store, null!, null!, null!, null!, null!, null!, null!, null!);
        return userManager;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
