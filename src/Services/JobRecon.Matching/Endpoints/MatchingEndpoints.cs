using System.Security.Claims;
using JobRecon.Matching.Contracts;

namespace JobRecon.Matching.Endpoints;

public static class MatchingEndpoints
{
    public static void MapMatchingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/matching")
            .WithTags("Matching")
            .RequireAuthorization();

        group.MapGet("/recommendations", GetRecommendations)
            .WithName("GetRecommendations")
            .WithDescription("Get personalized job recommendations based on user profile")
            .Produces<RecommendationsResponse>()
            .Produces(401);

        group.MapGet("/jobs/{jobId:guid}/score", GetJobMatchScore)
            .WithName("GetJobMatchScore")
            .WithDescription("Get match score for a specific job")
            .Produces<JobRecommendation>()
            .Produces(401)
            .Produces(404);
    }

    private static async Task<IResult> GetRecommendations(
        IMatchingService matchingService,
        ClaimsPrincipal user,
        int? pageSize,
        int? page,
        double? minScore,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId == null)
            return Results.Unauthorized();

        var request = new GetRecommendationsRequest(
            Math.Clamp(pageSize ?? 20, 1, 100),
            Math.Max(1, page ?? 1),
            minScore ?? 0.0);

        var result = await matchingService.GetRecommendationsAsync(
            userId.Value,
            request,
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetJobMatchScore(
        Guid jobId,
        IMatchingService matchingService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId == null)
            return Results.Unauthorized();

        var result = await matchingService.GetJobMatchScoreAsync(
            userId.Value,
            jobId,
            cancellationToken);

        return result != null
            ? Results.Ok(result)
            : Results.NotFound();
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
