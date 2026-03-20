using JobRecon.Domain.Common;

namespace JobRecon.Jobs.Domain.Events;

public sealed record JobsFetchedEvent : DomainEvent
{
    public required Guid JobSourceId { get; init; }
    public required int JobCount { get; init; }
    public required int NewJobCount { get; init; }
}
