using Microsoft.AspNetCore.Identity;

namespace JobRecon.Identity.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = [];

    public string FullName => string.IsNullOrWhiteSpace(FirstName)
        ? Email ?? string.Empty
        : $"{FirstName} {LastName}".Trim();
}
