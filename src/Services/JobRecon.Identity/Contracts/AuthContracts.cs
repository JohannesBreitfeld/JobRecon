using System.ComponentModel.DataAnnotations;

namespace JobRecon.Identity.Contracts;

public sealed record RegisterRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    /// <summary>
    /// Password must be at least 6 characters and contain:
    /// - At least one uppercase letter
    /// - At least one lowercase letter
    /// - At least one digit
    /// - At least one non-alphanumeric character
    /// </summary>
    [Required]
    [MinLength(6)]
    public required string Password { get; init; }

    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}

public sealed record LoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }

    public string? DeviceInfo { get; init; }
}

public sealed record RefreshTokenRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}

public sealed record AuthResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime AccessTokenExpiration { get; init; }
    public required UserInfo User { get; init; }
}

public sealed record UserInfo
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public required bool EmailConfirmed { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
}
