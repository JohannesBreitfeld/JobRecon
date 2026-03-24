using JobRecon.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace JobRecon.Identity.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public string FullName => string.IsNullOrWhiteSpace(FirstName)
        ? Email ?? string.Empty
        : $"{FirstName} {LastName}".Trim();

    public void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
