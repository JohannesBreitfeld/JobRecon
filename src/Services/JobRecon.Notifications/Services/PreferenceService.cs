using JobRecon.Notifications.Contracts;
using JobRecon.Notifications.Domain;
using JobRecon.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Notifications.Services;

public sealed class PreferenceService : IPreferenceService
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ILogger<PreferenceService> _logger;

    public PreferenceService(
        NotificationsDbContext dbContext,
        ILogger<PreferenceService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<NotificationPreference> GetOrCreatePreferencesAsync(Guid userId, CancellationToken ct = default)
    {
        var preference = await _dbContext.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (preference is not null)
        {
            return preference;
        }

        preference = NotificationPreference.CreateDefault(userId);
        _dbContext.NotificationPreferences.Add(preference);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Created default notification preferences for user {UserId}", userId);

        return preference;
    }

    public async Task<NotificationPreference> UpdatePreferencesAsync(
        Guid userId,
        UpdatePreferencesRequest request,
        CancellationToken ct = default)
    {
        var preference = await GetOrCreatePreferencesAsync(userId, ct);

        preference.Update(
            request.EmailEnabled,
            request.InAppEnabled,
            request.DigestEnabled,
            request.DigestFrequency,
            request.DigestTime,
            request.MinMatchScoreForRealtime,
            request.OverrideEmail);

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Updated notification preferences for user {UserId}", userId);

        return preference;
    }

    public async Task<List<NotificationPreference>> GetUsersReadyForDigestAsync(
        DigestFrequency frequency,
        TimeOnly currentTime,
        CancellationToken ct = default)
    {
        // Get users whose digest time has passed in the current hour window
        var hourStart = new TimeOnly(currentTime.Hour, 0);
        var hourEnd = new TimeOnly(currentTime.Hour, 59, 59);

        return await _dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(p => p.DigestEnabled
                && p.DigestFrequency == frequency
                && p.DigestTime >= hourStart
                && p.DigestTime <= hourEnd)
            .ToListAsync(ct);
    }
}
