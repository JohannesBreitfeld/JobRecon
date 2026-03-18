using JobRecon.Domain.Common;

namespace JobRecon.Domain.Identity.Events;

public sealed record EmailConfirmedEvent(Guid UserId, string Email) : DomainEvent;
