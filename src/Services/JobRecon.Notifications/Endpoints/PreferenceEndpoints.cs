using System.Security.Claims;
using JobRecon.Notifications.Contracts;
using JobRecon.Notifications.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobRecon.Notifications.Endpoints;

public static class PreferenceEndpoints
{
    public static void MapPreferenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications/preferences")
            .RequireAuthorization()
            .WithTags("Notification Preferences");

        group.MapGet("", GetPreferences)
            .WithName("GetNotificationPreferences")
            .WithSummary("Get user's notification preferences")
            .Produces<NotificationPreferenceDto>();

        group.MapPut("", UpdatePreferences)
            .WithName("UpdateNotificationPreferences")
            .WithSummary("Update user's notification preferences")
            .Produces<NotificationPreferenceDto>();
    }

    private static async Task<IResult> GetPreferences(
        [FromServices] IPreferenceService preferenceService,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var preferences = await preferenceService.GetOrCreatePreferencesAsync(userId.Value, ct);

        return Results.Ok(MapToDto(preferences));
    }

    private static async Task<IResult> UpdatePreferences(
        [FromServices] IPreferenceService preferenceService,
        [FromBody] UpdatePreferencesRequest request,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var preferences = await preferenceService.UpdatePreferencesAsync(userId.Value, request, ct);

        return Results.Ok(MapToDto(preferences));
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static NotificationPreferenceDto MapToDto(Domain.NotificationPreference preference)
    {
        return new NotificationPreferenceDto(
            preference.EmailEnabled,
            preference.InAppEnabled,
            preference.DigestEnabled,
            preference.DigestFrequency,
            preference.DigestTime,
            preference.MinMatchScoreForRealtime,
            preference.OverrideEmail);
    }
}
