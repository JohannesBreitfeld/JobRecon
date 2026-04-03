using System.Text.Json;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace JobRecon.Jobs.Services;

public interface ILocalityService
{
    Task<List<LocalityResponse>> SearchAsync(string? query, int limit = 20, CancellationToken ct = default);
    Task<List<Locality>> GetAllForGeocodingAsync(CancellationToken ct = default);
}

public sealed class LocalityService(
    JobsDbContext dbContext,
    IDistributedCache cache,
    ILogger<LocalityService> logger) : ILocalityService
{
    private const string CitiesCacheKey = "localities:cities";
    private const string GeocodingCacheKey = "localities:geocoding";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public async Task<List<LocalityResponse>> SearchAsync(string? query, int limit = 20, CancellationToken ct = default)
    {
        var cities = await GetCachedCitiesAsync(ct);

        if (string.IsNullOrWhiteSpace(query))
            return cities.Take(limit).ToList();

        var q = query.Trim().ToLowerInvariant();

        return cities
            .Where(c => c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                         c.AsciiName.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .ToList();
    }

    public async Task<List<Locality>> GetAllForGeocodingAsync(CancellationToken ct = default)
    {
        try
        {
            var bytes = await cache.GetAsync(GeocodingCacheKey, ct);
            if (bytes is not null)
                return JsonSerializer.Deserialize<List<Locality>>(bytes) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read geocoding localities from cache");
        }

        var localities = await dbContext.Localities
            .AsNoTracking()
            .ToListAsync(ct);

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(localities);
            await cache.SetAsync(GeocodingCacheKey, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cache geocoding localities");
        }

        return localities;
    }

    private async Task<List<LocalityResponse>> GetCachedCitiesAsync(CancellationToken ct)
    {
        try
        {
            var bytes = await cache.GetAsync(CitiesCacheKey, ct);
            if (bytes is not null)
                return JsonSerializer.Deserialize<List<LocalityResponse>>(bytes) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read cities from cache");
        }

        var cities = await dbContext.Localities
            .AsNoTracking()
            .Where(l => l.Population > 500)
            .Where(l => l.FeatureCode == "PPL" || l.FeatureCode == "PPLA" ||
                         l.FeatureCode == "PPLA2" || l.FeatureCode == "PPLA3" ||
                         l.FeatureCode == "PPLC" || l.FeatureCode == "PPLX")
            .OrderByDescending(l => l.Population)
            .Select(l => new LocalityResponse(
                l.GeoNameId, l.Name, l.AsciiName, l.Latitude, l.Longitude, l.Population))
            .ToListAsync(ct);

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(cities);
            await cache.SetAsync(CitiesCacheKey, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cache cities");
        }

        return cities;
    }
}
