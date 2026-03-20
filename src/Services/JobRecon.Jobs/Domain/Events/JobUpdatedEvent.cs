using JobRecon.Domain.Common;

namespace JobRecon.Jobs.Domain.Events;

public sealed record JobUpdatedEvent : DomainEvent
{
    public required Guid JobId { get; init; }
}
