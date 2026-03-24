using JobRecon.Notifications.Contracts;
using JobRecon.Notifications.Domain;

namespace JobRecon.Notifications.Services;

public interface IPreferenceService
{
    Task<NotificationPreference> GetOrCreatePreferencesAsync(Guid userId, CancellationToken ct = default);

    Task<NotificationPreference> UpdatePreferencesAsync(
        Guid userId,
        UpdatePreferencesRequest request,
        CancellationToken ct = default);

    Task<List<NotificationPreference>> GetUsersReadyForDigestAsync(
        DigestFrequency frequency,
        TimeOnly currentTime,
        CancellationToken ct = default);

    Task<bool> UnsubscribeByTokenAsync(string token, CancellationToken ct = default);
}
