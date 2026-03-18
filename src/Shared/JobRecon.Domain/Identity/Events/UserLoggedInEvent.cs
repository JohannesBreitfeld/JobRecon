using JobRecon.Domain.Common;

namespace JobRecon.Domain.Identity.Events;

public sealed record UserLoggedInEvent(Guid UserId) : DomainEvent;
