using System.Text.Json;
using JobRecon.Notifications.Contracts;
using JobRecon.Notifications.Domain;
using JobRecon.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace JobRecon.Notifications.Services;

public sealed class PreferenceService : IPreferenceService
{
    private readonly NotificationsDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PreferenceService> _logger;

    private static readonly TimeSpan PreferencesTtl = TimeSpan.FromHours(1);

    public PreferenceService(
        NotificationsDbContext dbContext,
        IDistributedCache cache,
        ILogger<PreferenceService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<NotificationPreference> GetOrCreatePreferencesAsync(Guid userId, CancellationToken ct = default)
    {
        var key = $"notif:prefs:{userId}";

        try
        {
            var cached = await _cache.GetStringAsync(key, ct);
            if (cached is not null)
            {
                var deserialized = JsonSerializer.Deserialize<NotificationPreference>(cached);
                if (deserialized is not null)
                    return deserialized;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis read failed for preferences key {Key}", key);
        }

        var preference = await _dbContext.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (preference is null)
        {
            preference = NotificationPreference.CreateDefault(userId);
            _dbContext.NotificationPreferences.Add(preference);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Created default notification preferences for user {UserId}", userId);
        }

        try
        {
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(preference), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = PreferencesTtl
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis write failed for preferences key {Key}", key);
        }

        return preference;
    }

    public async Task<NotificationPreference> UpdatePreferencesAsync(
        Guid userId,
        UpdatePreferencesRequest request,
        CancellationToken ct = default)
    {
        var preference = await _dbContext.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (preference is null)
        {
            preference = NotificationPreference.CreateDefault(userId);
            _dbContext.NotificationPreferences.Add(preference);
        }

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

        await InvalidatePreferencesCacheAsync(userId);

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

    public async Task<bool> UnsubscribeByTokenAsync(string token, CancellationToken ct = default)
    {
        var preference = await _dbContext.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UnsubscribeToken == token, ct);

        if (preference is null)
        {
            return false;
        }

        preference.EmailEnabled = false;
        preference.DigestEnabled = false;
        preference.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} unsubscribed from emails via token", preference.UserId);

        await InvalidatePreferencesCacheAsync(preference.UserId);

        return true;
    }

    private async Task InvalidatePreferencesCacheAsync(Guid userId)
    {
        try
        {
            await _cache.RemoveAsync($"notif:prefs:{userId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate preferences cache for user {UserId}", userId);
        }
    }
}
