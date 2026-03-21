namespace JobRecon.Notifications.Contracts;

public record JobMatchEmailDto(
    Guid JobId,
    string JobTitle,
    string CompanyName,
    string? Location,
    double MatchScore,
    List<MatchFactorData> TopFactors,
    string? JobUrl);

public record DigestItemDto(
    Guid JobId,
    string JobTitle,
    string CompanyName,
    string? Location,
    double MatchScore,
    string? TopMatchFactors,
    string? JobUrl);

public record DigestEmailDto(
    List<DigestItemDto> Jobs,
    int TotalJobCount,
    DateTime PeriodStart,
    DateTime PeriodEnd);
