using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Jobs.Services;

public interface ILocalityImportService
{
    Task<int> ImportFromGeoNamesFileAsync(string filePath, CancellationToken ct = default);
}

public sealed class LocalityImportService(
    JobsDbContext dbContext,
    ILogger<LocalityImportService> logger) : ILocalityImportService
{
    private static readonly HashSet<string> AllowedFeatureCodes =
    [
        // Populated places
        "PPL", "PPLA", "PPLA2", "PPLA3", "PPLC", "PPLX", "PPLL", "PPLF", "PPLR",
        // Administrative divisions
        "ADM2", "ADM3"
    ];

    public async Task<int> ImportFromGeoNamesFileAsync(string filePath, CancellationToken ct = default)
    {
        logger.LogInformation("Starting locality import from {FilePath}", filePath);

        var localities = ParseGeoNamesFile(filePath);

        logger.LogInformation("Parsed {Count} localities from file", localities.Count);

        var existingIds = await dbContext.Localities
            .Select(l => l.GeoNameId)
            .ToHashSetAsync(ct);

        var toInsert = new List<Locality>();
        var toUpdate = new List<Locality>();

        foreach (var locality in localities)
        {
            if (existingIds.Contains(locality.GeoNameId))
                toUpdate.Add(locality);
            else
                toInsert.Add(locality);
        }

        if (toInsert.Count > 0)
        {
            dbContext.Localities.AddRange(toInsert);
        }

        foreach (var locality in toUpdate)
        {
            var existing = await dbContext.Localities.FindAsync([locality.GeoNameId], ct);
            if (existing is null) continue;

            existing.Name = locality.Name;
            existing.AsciiName = locality.AsciiName;
            existing.AlternateNames = locality.AlternateNames;
            existing.Latitude = locality.Latitude;
            existing.Longitude = locality.Longitude;
            existing.FeatureCode = locality.FeatureCode;
            existing.Admin2Code = locality.Admin2Code;
            existing.Population = locality.Population;
        }

        var saved = await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Imported {Inserted} new, updated {Updated} existing localities",
            toInsert.Count, toUpdate.Count);

        return toInsert.Count + toUpdate.Count;
    }

    private static List<Locality> ParseGeoNamesFile(string filePath)
    {
        var localities = new List<Locality>();

        foreach (var line in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = line.Split('\t');
            if (fields.Length < 19) continue;

            var featureClass = fields[6];
            var featureCode = fields[7];

            // Filter: populated places (P) or administrative (A) with allowed codes
            if (featureClass is not ("P" or "A")) continue;
            if (!AllowedFeatureCodes.Contains(featureCode)) continue;

            if (!int.TryParse(fields[0], out var geoNameId)) continue;
            if (!double.TryParse(fields[4], System.Globalization.CultureInfo.InvariantCulture, out var latitude)) continue;
            if (!double.TryParse(fields[5], System.Globalization.CultureInfo.InvariantCulture, out var longitude)) continue;
            _ = int.TryParse(fields[14], out var population);

            var name = fields[1];
            var asciiName = fields[2];
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(asciiName)) continue;

            localities.Add(new Locality
            {
                GeoNameId = geoNameId,
                Name = name,
                AsciiName = asciiName,
                AlternateNames = string.IsNullOrWhiteSpace(fields[3]) ? null : fields[3],
                Latitude = latitude,
                Longitude = longitude,
                FeatureCode = featureCode,
                Admin2Code = string.IsNullOrWhiteSpace(fields[11]) ? null : fields[11],
                Population = population
            });
        }

        return localities;
    }
}
