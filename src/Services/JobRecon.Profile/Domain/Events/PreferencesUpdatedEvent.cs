using JobRecon.Domain.Common;

namespace JobRecon.Profile.Domain.Events;

public sealed record PreferencesUpdatedEvent : DomainEvent
{
    public required Guid UserId { get; init; }
    public required Guid ProfileId { get; init; }
}
