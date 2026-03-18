using JobRecon.Domain.Common;

namespace JobRecon.Domain.Identity.Events;

public sealed record PasswordChangedEvent(Guid UserId) : DomainEvent;
