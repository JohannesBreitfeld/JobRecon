using JobRecon.Jobs.Domain;

namespace JobRecon.Jobs.Services;

public sealed record GeocodingResult(int GeoNameId, double Latitude, double Longitude);

public interface IGeocodingService
{
    Task<GeocodingResult?> GeocodeAsync(string locationText, CancellationToken ct = default);
}

public sealed class GeocodingService(
    ILocalityService localityService,
    ILogger<GeocodingService> logger) : IGeocodingService
{
    private List<Locality>? _localities;
    private Dictionary<string, Locality>? _exactLookup;

    public async Task<GeocodingResult?> GeocodeAsync(string locationText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(locationText))
            return null;

        await EnsureLoadedAsync(ct);

        var normalized = locationText.Trim().ToLowerInvariant();

        // Pass 1: Exact match against name, ascii name, or alternate names
        if (_exactLookup!.TryGetValue(normalized, out var exact))
            return ToResult(exact);

        // Pass 2: Token extraction — split on commas, semicolons, dashes, and try each token
        var tokens = normalized
            .Split([',', ';', '-', '/', '(', ')'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 2)
            .ToList();

        Locality? bestMatch = null;

        foreach (var token in tokens)
        {
            if (_exactLookup.TryGetValue(token, out var tokenMatch))
            {
                if (bestMatch is null || tokenMatch.Population > bestMatch.Population)
                    bestMatch = tokenMatch;
            }
        }

        if (bestMatch is not null)
            return ToResult(bestMatch);

        // Pass 3: Space-split tokens (handles "Senior Developer Stockholm")
        var spaceTokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 2);

        foreach (var token in spaceTokens)
        {
            if (_exactLookup.TryGetValue(token, out var spaceMatch))
            {
                if (bestMatch is null || spaceMatch.Population > bestMatch.Population)
                    bestMatch = spaceMatch;
            }
        }

        if (bestMatch is not null)
            return ToResult(bestMatch);

        logger.LogDebug("Could not geocode location: {LocationText}", locationText);
        return null;
    }

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_localities is not null) return;

        _localities = await localityService.GetAllForGeocodingAsync(ct);
        _exactLookup = BuildExactLookup(_localities);
    }

    private static Dictionary<string, Locality> BuildExactLookup(List<Locality> localities)
    {
        var lookup = new Dictionary<string, Locality>(StringComparer.OrdinalIgnoreCase);

        // Process in ascending population order so larger cities win on collision
        foreach (var locality in localities.OrderBy(l => l.Population))
        {
            lookup[locality.Name.ToLowerInvariant()] = locality;
            lookup[locality.AsciiName.ToLowerInvariant()] = locality;

            if (string.IsNullOrEmpty(locality.AlternateNames)) continue;

            foreach (var alt in locality.AlternateNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (alt.Length > 2)
                    lookup[alt.ToLowerInvariant()] = locality;
            }
        }

        return lookup;
    }

    private static GeocodingResult ToResult(Locality l) => new(l.GeoNameId, l.Latitude, l.Longitude);
}
