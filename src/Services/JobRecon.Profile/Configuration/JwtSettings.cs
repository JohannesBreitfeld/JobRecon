using System.ComponentModel.DataAnnotations;

namespace JobRecon.Profile.Configuration;

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
}
