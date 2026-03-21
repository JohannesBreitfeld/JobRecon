namespace JobRecon.Notifications.Contracts;

public record JobMatchedEvent(
    Guid EventId,
    Guid UserId,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    string? Location,
    double MatchScore,
    List<MatchFactorEvent> TopFactors,
    string? JobUrl,
    DateTime MatchedAt);

public record MatchFactorEvent(
    string Category,
    double Score,
    string? Description);
