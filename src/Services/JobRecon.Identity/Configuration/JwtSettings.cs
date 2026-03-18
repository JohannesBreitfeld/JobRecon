using System.ComponentModel.DataAnnotations;

namespace JobRecon.Identity.Configuration;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = null!;

    [Required]
    public string Audience { get; set; } = null!;

    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = null!;

    [Range(1, 1440)]
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    [Range(1, 43200)]
    public int RefreshTokenExpirationMinutes { get; set; } = 10080; // 7 days
}
