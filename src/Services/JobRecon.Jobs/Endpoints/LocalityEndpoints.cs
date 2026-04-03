using JobRecon.Jobs.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobRecon.Jobs.Endpoints;

public static class LocalityEndpoints
{
    public static void MapLocalityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/localities")
            .WithTags("Localities");

        group.MapGet("/search", SearchLocalities)
            .WithName("SearchLocalities")
            .WithSummary("Search localities for autocomplete");
    }

    private static async Task<IResult> SearchLocalities(
        [FromQuery(Name = "q")] string? query,
        [FromQuery] int limit,
        ILocalityService localityService,
        CancellationToken ct)
    {
        if (limit <= 0) limit = 20;
        if (limit > 100) limit = 100;

        var results = await localityService.SearchAsync(query, limit, ct);
        return Results.Ok(results);
    }
}
