namespace JobRecon.Contracts.Events;

public record JobsExpiredIntegrationEvent(
    Guid EventId,
    IReadOnlyList<Guid> JobIds,
    DateTime ExpiredAt);
