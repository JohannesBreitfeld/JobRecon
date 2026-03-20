using System.Formats.Tar;
using System.IO.Compression;
using System.Text.Json;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using Polly;
using Polly.Retry;

namespace JobRecon.Jobs.Services.Fetchers;

public sealed class JobTechLinksFetcher : IJobFetcher
{
    private const string BaseDownloadUrl = "https://data.jobtechdev.se/annonser/jobtechlinks";
    private readonly HttpClient _httpClient;
    private readonly ILogger<JobTechLinksFetcher> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public JobSourceType SourceType => JobSourceType.JobTechLinks;

    public JobTechLinksFetcher(
        HttpClient httpClient,
        ILogger<JobTechLinksFetcher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && r.StatusCode != System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<List<FetchedJob>> FetchJobsAsync(
        JobSource source,
        CancellationToken cancellationToken = default)
    {
        var jobs = new List<FetchedJob>();

        try
        {
            var config = !string.IsNullOrEmpty(source.Configuration)
                ? JsonSerializer.Deserialize<JobTechLinksConfig>(source.Configuration)
                : new JobTechLinksConfig();

            config ??= new JobTechLinksConfig();

            // Determine which dates to fetch
            var datesToFetch = GetDatesToFetch(config);

            foreach (var date in datesToFetch)
            {
                var dateStr = date.ToString("yyyy-MM-dd");
                var downloadUrl = $"{BaseDownloadUrl}/{dateStr}.tar.gz";

                _logger.LogInformation("Downloading JobTech Links file for {Date}", dateStr);

                var fetchedJobs = await DownloadAndParseAsync(downloadUrl, cancellationToken);

                if (fetchedJobs.Count > 0)
                {
                    jobs.AddRange(fetchedJobs);
                    config.LastDownloadedDate = dateStr;
                    _logger.LogInformation("Processed {Count} jobs from {Date}", fetchedJobs.Count, dateStr);
                }
            }

            // Update source configuration with last downloaded date
            source.Configuration = JsonSerializer.Serialize(config);

            _logger.LogInformation("Total fetched {Count} jobs from JobTech Links", jobs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from JobTech Links");
            throw;
        }

        return jobs;
    }

    private static List<DateOnly> GetDatesToFetch(JobTechLinksConfig config)
    {
        var dates = new List<DateOnly>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Start from last downloaded date + 1, or 7 days ago
        DateOnly startDate;
        if (!string.IsNullOrEmpty(config.LastDownloadedDate) &&
            DateOnly.TryParse(config.LastDownloadedDate, out var lastDownloaded))
        {
            startDate = lastDownloaded.AddDays(1);
        }
        else
        {
            startDate = today.AddDays(-config.MaxDaysToFetch);
        }

        // Don't fetch future dates, and limit to today (files usually available next day)
        var endDate = today.AddDays(-1); // Yesterday's file is most likely available

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            dates.Add(date);
        }

        return dates;
    }

    private async Task<List<FetchedJob>> DownloadAndParseAsync(
        string url,
        CancellationToken cancellationToken)
    {
        var jobs = new List<FetchedJob>();

        try
        {
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            });

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found: {Url}", url);
                return jobs;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download {Url}: {StatusCode}", url, response.StatusCode);
                return jobs;
            }

            // Stream to temp file to avoid memory issues with large files
            var tempFile = Path.GetTempFileName();
            try
            {
                await using (var fileStream = File.Create(tempFile))
                await using (var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                {
                    await downloadStream.CopyToAsync(fileStream, cancellationToken);
                }

                jobs = await ExtractAndParseAsync(tempFile, cancellationToken);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading/parsing {Url}", url);
        }

        return jobs;
    }

    private async Task<List<FetchedJob>> ExtractAndParseAsync(
        string tarGzPath,
        CancellationToken cancellationToken)
    {
        var jobs = new List<FetchedJob>();
        var extractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            Directory.CreateDirectory(extractDir);

            // Decompress gzip
            var tarPath = Path.Combine(extractDir, "archive.tar");
            await using (var gzipStream = File.OpenRead(tarGzPath))
            await using (var decompressedStream = new GZipStream(gzipStream, CompressionMode.Decompress))
            await using (var tarFileStream = File.Create(tarPath))
            {
                await decompressedStream.CopyToAsync(tarFileStream, cancellationToken);
            }

            // Extract tar
            await TarFile.ExtractToDirectoryAsync(tarPath, extractDir, overwriteFiles: true, cancellationToken);

            // Find and parse JSON files
            foreach (var jsonFile in Directory.GetFiles(extractDir, "*.json", SearchOption.AllDirectories))
            {
                var content = await File.ReadAllTextAsync(jsonFile, cancellationToken);

                // Try parsing as response with hits array
                try
                {
                    var response = JsonSerializer.Deserialize<JobTechLinksResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (response?.Hits != null)
                    {
                        foreach (var hit in response.Hits)
                        {
                            if (!hit.Removed)
                            {
                                jobs.Add(MapToFetchedJob(hit));
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Try parsing as array of hits directly
                    try
                    {
                        var hits = JsonSerializer.Deserialize<List<JobTechLinksHit>>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (hits != null)
                        {
                            foreach (var hit in hits)
                            {
                                if (!hit.Removed)
                                {
                                    jobs.Add(MapToFetchedJob(hit));
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse JSON file: {File}", jsonFile);
                    }
                }
            }
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(extractDir))
            {
                Directory.Delete(extractDir, recursive: true);
            }
        }

        return jobs;
    }

    private static FetchedJob MapToFetchedJob(JobTechLinksHit hit)
    {
        var primaryAddress = hit.WorkplaceAddresses?.FirstOrDefault();
        var primarySourceLink = hit.SourceLinks?.FirstOrDefault();

        return new FetchedJob
        {
            ExternalId = hit.Id,
            Title = hit.Headline ?? "Untitled",
            Description = hit.Description?.Text ?? hit.Brief,
            CompanyName = hit.Employer?.Name ?? "Unknown",
            CompanyLogoUrl = hit.LogoUrl,
            CompanyWebsite = hit.Employer?.Url,
            Location = GetLocation(primaryAddress),
            WorkLocationType = null, // Not directly available in JobTech Links
            EmploymentType = ParseEmploymentType(hit.EmploymentType?.Label, hit.WorkingHoursType?.Label),
            SalaryMin = null,
            SalaryMax = null,
            SalaryCurrency = "SEK",
            SalaryPeriod = hit.SalaryType?.Label,
            ExternalUrl = primarySourceLink?.Url,
            ApplicationUrl = primarySourceLink?.Url,
            RequiredSkills = GetRequiredSkills(hit.MustHave),
            Benefits = null,
            ExperienceYearsMin = hit.ExperienceRequired == true ? 1 : null,
            ExperienceYearsMax = null,
            PostedAt = hit.PublicationDate ?? hit.LastPublicationDate,
            ExpiresAt = hit.ApplicationDeadline,
            Tags = ExtractTags(hit)
        };
    }

    private static string? GetLocation(JobTechLinksAddress? address)
    {
        if (address == null) return null;

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(address.City))
            parts.Add(address.City);
        else if (!string.IsNullOrEmpty(address.Municipality))
            parts.Add(address.Municipality);

        if (!string.IsNullOrEmpty(address.Region))
            parts.Add(address.Region);

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    private static EmploymentType? ParseEmploymentType(string? employmentType, string? workingHoursType)
    {
        var combined = $"{employmentType} {workingHoursType}".ToLowerInvariant();

        return combined switch
        {
            var t when t.Contains("heltid") => EmploymentType.FullTime,
            var t when t.Contains("deltid") => EmploymentType.PartTime,
            var t when t.Contains("vikariat") || t.Contains("tidsbegränsad") => EmploymentType.Temporary,
            var t when t.Contains("praktik") => EmploymentType.Internship,
            var t when t.Contains("frilans") || t.Contains("konsult") => EmploymentType.Freelance,
            _ => null
        };
    }

    private static string? GetRequiredSkills(JobTechLinksRequirements? requirements)
    {
        if (requirements == null) return null;

        var skills = new List<string>();

        if (requirements.Skills != null)
            skills.AddRange(requirements.Skills.Select(s => s.Label).Where(l => !string.IsNullOrEmpty(l))!);

        if (requirements.Languages != null)
            skills.AddRange(requirements.Languages.Select(l => l.Label).Where(l => !string.IsNullOrEmpty(l))!);

        return skills.Count > 0 ? string.Join(", ", skills) : null;
    }

    private static List<string> ExtractTags(JobTechLinksHit hit)
    {
        var tags = new List<string>();

        // Add occupation info
        if (!string.IsNullOrEmpty(hit.Occupation?.Label))
            tags.Add(hit.Occupation.Label);

        if (!string.IsNullOrEmpty(hit.OccupationGroup?.Label))
            tags.Add(hit.OccupationGroup.Label);

        if (!string.IsNullOrEmpty(hit.OccupationField?.Label))
            tags.Add(hit.OccupationField.Label);

        // Add required skills
        if (hit.MustHave?.Skills != null)
            tags.AddRange(hit.MustHave.Skills.Select(s => s.Label).Where(l => !string.IsNullOrEmpty(l))!);

        // Add nice-to-have skills
        if (hit.NiceToHave?.Skills != null)
            tags.AddRange(hit.NiceToHave.Skills.Select(s => s.Label).Where(l => !string.IsNullOrEmpty(l))!);

        // Add languages
        if (hit.MustHave?.Languages != null)
            tags.AddRange(hit.MustHave.Languages.Select(l => l.Label).Where(l => !string.IsNullOrEmpty(l))!);

        return tags.Distinct().Take(30).ToList();
    }
}
