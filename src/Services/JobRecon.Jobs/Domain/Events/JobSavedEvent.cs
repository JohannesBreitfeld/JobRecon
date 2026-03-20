using JobRecon.Domain.Common;

namespace JobRecon.Jobs.Domain.Events;

public sealed record JobSavedEvent : DomainEvent
{
    public required Guid UserId { get; init; }
    public required Guid JobId { get; init; }
}
