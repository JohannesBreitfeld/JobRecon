using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Jobs.Services;

public interface IGeocodingBackfillService
{
    Task<int> BackfillAsync(int batchSize = 100, CancellationToken ct = default);
}

public sealed class GeocodingBackfillService(
    JobsDbContext dbContext,
    IGeocodingService geocodingService,
    ILogger<GeocodingBackfillService> logger) : IGeocodingBackfillService
{
    public async Task<int> BackfillAsync(int batchSize = 100, CancellationToken ct = default)
    {
        var jobs = await dbContext.Jobs
            .Where(j => j.Location != null && j.LocalityId == null)
            .OrderBy(j => j.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        if (jobs.Count == 0)
        {
            logger.LogDebug("No jobs need geocoding backfill");
            return 0;
        }

        var geocoded = 0;

        foreach (var job in jobs)
        {
            var result = await geocodingService.GeocodeAsync(job.Location!, ct);
            if (result is not null)
            {
                job.LocalityId = result.GeoNameId;
                job.Latitude = result.Latitude;
                job.Longitude = result.Longitude;
                geocoded++;
            }
        }

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Geocoded {Count}/{Total} jobs in backfill batch", geocoded, jobs.Count);

        return geocoded;
    }
}
