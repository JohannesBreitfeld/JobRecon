using JobRecon.Domain.Common;

namespace JobRecon.Profile.Domain.Events;

public sealed record CVUploadedEvent : DomainEvent
{
    public required Guid UserId { get; init; }
    public required Guid ProfileId { get; init; }
    public required Guid DocumentId { get; init; }
    public required string FileName { get; init; }
    public required string StoragePath { get; init; }
}
