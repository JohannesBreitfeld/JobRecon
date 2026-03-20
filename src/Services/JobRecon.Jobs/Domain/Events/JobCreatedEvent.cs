using JobRecon.Domain.Common;

namespace JobRecon.Jobs.Domain.Events;

public sealed record JobCreatedEvent : DomainEvent
{
    public required Guid JobId { get; init; }
    public required string Title { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid JobSourceId { get; init; }
}
