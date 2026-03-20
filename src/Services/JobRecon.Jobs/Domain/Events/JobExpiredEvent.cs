using JobRecon.Domain.Common;

namespace JobRecon.Jobs.Domain.Events;

public sealed record JobExpiredEvent : DomainEvent
{
    public required Guid JobId { get; init; }
}
