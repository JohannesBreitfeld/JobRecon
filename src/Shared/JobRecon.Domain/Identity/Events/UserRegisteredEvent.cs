using JobRecon.Domain.Common;

namespace JobRecon.Domain.Identity.Events;

public sealed record UserRegisteredEvent(Guid UserId, string Email) : DomainEvent;
