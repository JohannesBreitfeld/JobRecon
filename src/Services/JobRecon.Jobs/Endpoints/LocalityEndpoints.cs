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

        group.MapPost("/backfill-geocoding", TriggerGeocodingBackfill)
            .WithName("TriggerGeocodingBackfill")
            .WithSummary("Trigger geocoding backfill for existing jobs")
;
    }

    private static async Task<IResult> TriggerGeocodingBackfill(
        IGeocodingBackfillService backfillService,
        CancellationToken ct,
        [FromQuery] int batchSize = 5000)
    {
        if (batchSize <= 0) batchSize = 5000;
        if (batchSize > 50000) batchSize = 50000;

        var geocoded = await backfillService.BackfillAsync(batchSize, ct);
        return Results.Ok(new { geocoded, batchSize });
    }

    private static async Task<IResult> SearchLocalities(
        ILocalityService localityService,
        CancellationToken ct,
        [FromQuery(Name = "q")] string? query = null,
        [FromQuery] int limit = 20)
    {
        if (limit <= 0) limit = 20;
        if (limit > 100) limit = 100;

        var results = await localityService.SearchAsync(query, limit, ct);
        return Results.Ok(results);
    }
}
