using System.Security.Claims;
using JobRecon.Notifications.Contracts;
using JobRecon.Notifications.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobRecon.Notifications.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .RequireAuthorization()
            .WithTags("Notifications");

        group.MapGet("", GetNotifications)
            .WithName("GetNotifications")
            .WithSummary("Get user's notifications")
            .Produces<NotificationsResponse>();

        group.MapGet("/unread-count", GetUnreadCount)
            .WithName("GetUnreadCount")
            .WithSummary("Get unread notification count")
            .Produces<UnreadCountResponse>();

        group.MapPost("/{id:guid}/read", MarkAsRead)
            .WithName("MarkNotificationAsRead")
            .WithSummary("Mark a notification as read")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/read-all", MarkAllAsRead)
            .WithName("MarkAllNotificationsAsRead")
            .WithSummary("Mark all notifications as read")
            .Produces<MarkAllReadResponse>();
    }

    private static async Task<IResult> GetNotifications(
        [FromServices] INotificationService notificationService,
        ClaimsPrincipal user,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? unreadOnly = null,
        CancellationToken ct = default)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var result = await notificationService.GetUserNotificationsAsync(
            userId.Value, page, pageSize, unreadOnly, ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetUnreadCount(
        [FromServices] INotificationService notificationService,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var count = await notificationService.GetUnreadCountAsync(userId.Value, ct);

        return Results.Ok(new UnreadCountResponse(count));
    }

    private static async Task<IResult> MarkAsRead(
        [FromServices] INotificationService notificationService,
        ClaimsPrincipal user,
        Guid id,
        CancellationToken ct = default)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var success = await notificationService.MarkAsReadAsync(userId.Value, id, ct);

        return success ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> MarkAllAsRead(
        [FromServices] INotificationService notificationService,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var count = await notificationService.MarkAllAsReadAsync(userId.Value, ct);

        return Results.Ok(new MarkAllReadResponse(count));
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public record MarkAllReadResponse(int MarkedCount);
