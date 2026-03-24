namespace JobRecon.Contracts.Events;

public record JobsFetchedIntegrationEvent(
    Guid EventId,
    Guid JobSourceId,
    int JobCount,
    int NewJobCount,
    DateTime FetchedAt);
