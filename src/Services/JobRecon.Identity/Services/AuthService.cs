using JobRecon.Domain.Common;
using JobRecon.Domain.Identity.Events;
using JobRecon.Identity.Contracts;
using JobRecon.Identity.Domain;
using JobRecon.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Identity.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IdentityDbContext dbContext,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Result.Failure<AuthResponse>(
                Error.Conflict("User.EmailExists", "A user with this email already exists."));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Failed to create user {Email}: {Errors}", request.Email, errors);
            return Result.Failure<AuthResponse>(
                Error.Validation("User.CreationFailed", errors));
        }

        await _userManager.AddToRoleAsync(user, Domain.Roles.User);

        user.RaiseDomainEvent(new UserRegisteredEvent(user.Id, user.Email!));

        _logger.LogInformation("User {Email} registered successfully", request.Email);

        return await GenerateAuthResponseAsync(user, null, cancellationToken);
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.AccountDeactivated", "This account has been deactivated."));
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.RaiseDomainEvent(new UserLoggedInEvent(user.Id));
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Email} logged in successfully", request.Email);

        return await GenerateAuthResponseAsync(user, request.DeviceInfo, cancellationToken);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashToken(request.RefreshToken);

        var refreshToken = await _dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Token.Invalid", "Invalid refresh token."));
        }

        if (!refreshToken.IsActive)
        {
            // Possible token reuse attack - revoke all tokens for this user
            if (refreshToken.IsRevoked)
            {
                _logger.LogWarning(
                    "Refresh token reuse detected for user {UserId}. Revoking all tokens.",
                    refreshToken.UserId);

                await RevokeAllUserTokensAsync(refreshToken.UserId, cancellationToken);
            }

            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Token.Invalid", "Refresh token is no longer valid."));
        }

        var user = refreshToken.User;
        if (!user.IsActive)
        {
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.AccountDeactivated", "This account has been deactivated."));
        }

        // Rotate refresh token
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newTokenHash = _tokenService.HashToken(newRefreshToken);

        refreshToken.Revoke(newTokenHash);

        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
            DeviceInfo = refreshToken.DeviceInfo,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(newToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);

        return Result.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiration = _tokenService.GetAccessTokenExpiration(),
            User = MapToUserInfo(user, roles)
        });
    }

    public async Task<Result> LogoutAsync(
        Guid userId,
        string? refreshToken = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var tokenHash = _tokenService.HashToken(refreshToken);
            var token = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(r => r.TokenHash == tokenHash && r.UserId == userId, cancellationToken);

            if (token is not null)
            {
                token.Revoke();
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return Result.Success();
    }

    public async Task<Result> RevokeAllTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await RevokeAllUserTokensAsync(userId, cancellationToken);
        return Result.Success();
    }

    private async Task<Result<AuthResponse>> GenerateAuthResponseAsync(
        ApplicationUser user,
        string? deviceInfo,
        CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var tokenHash = _tokenService.HashToken(refreshToken);

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = _tokenService.GetRefreshTokenExpiration(),
            DeviceInfo = deviceInfo,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiration = _tokenService.GetAccessTokenExpiration(),
            User = MapToUserInfo(user, roles)
        });
    }

    public async Task<Result> SendPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);

        // Always return success to prevent email enumeration
        if (user is null || !user.IsActive)
        {
            _logger.LogDebug("Password reset requested for unknown or inactive email");
            return Result.Success();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        _logger.LogInformation(
            "Password reset token generated for user {UserId}",
            user.Id);

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Result.Failure(Error.Validation("Auth.InvalidToken", "Invalid or expired reset token."));
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("Auth.ResetFailed", errors));
        }

        user.RaiseDomainEvent(new PasswordChangedEvent(user.Id));
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Password reset successfully for user {UserId}", user.Id);

        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.Validation("Auth.UserNotFound", "User not found."));
        }

        if (user.EmailConfirmed)
        {
            return Result.Success();
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("Auth.ConfirmationFailed", errors));
        }

        user.RaiseDomainEvent(new EmailConfirmedEvent(user.Id, user.Email!));
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Email confirmed for user {UserId}", user.Id);

        return Result.Success();
    }

    public async Task<Result> ResendEmailConfirmationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("Auth.UserNotFound", "User not found."));
        }

        if (user.EmailConfirmed)
        {
            return Result.Success();
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        _logger.LogInformation(
            "Email confirmation token generated for user {UserId}",
            user.Id);

        return Result.Success();
    }

    private async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static UserInfo MapToUserInfo(ApplicationUser user, IList<string> roles)
    {
        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToList()
        };
    }
}
