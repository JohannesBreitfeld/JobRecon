using JobRecon.Notifications.Domain;

namespace JobRecon.Notifications.Contracts;

public record NotificationPreferenceDto(
    bool EmailEnabled,
    bool InAppEnabled,
    bool DigestEnabled,
    DigestFrequency DigestFrequency,
    TimeOnly DigestTime,
    double MinMatchScoreForRealtime,
    string? OverrideEmail);

public record UpdatePreferencesRequest(
    bool? EmailEnabled = null,
    bool? InAppEnabled = null,
    bool? DigestEnabled = null,
    DigestFrequency? DigestFrequency = null,
    TimeOnly? DigestTime = null,
    double? MinMatchScoreForRealtime = null,
    string? OverrideEmail = null);
