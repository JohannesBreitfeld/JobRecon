using System.Collections.Concurrent;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using Polly;
using Polly.Retry;

namespace JobRecon.Jobs.Services.Fetchers;

public sealed class JobTechLinksFetcher : IJobFetcher
{
    private const string BaseDownloadUrl = "https://data.jobtechdev.se/annonser/jobtechlinks";
    private const int MaxConcurrentDownloads = 1;
    private readonly HttpClient _httpClient;
    private readonly ILogger<JobTechLinksFetcher> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public JobSourceType SourceType => JobSourceType.JobTechLinks;

    public JobTechLinksFetcher(
        HttpClient httpClient,
        ILogger<JobTechLinksFetcher> logger)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("JobRecon/1.0");
        _logger = logger;

        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && r.StatusCode != System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async IAsyncEnumerable<FetchedJobBatch> FetchJobBatchesAsync(
        JobSource source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var config = !string.IsNullOrEmpty(source.Configuration)
            ? JsonSerializer.Deserialize<JobTechLinksConfig>(source.Configuration)
            : new JobTechLinksConfig();

        config ??= new JobTechLinksConfig();

        var datesToFetch = GetDatesToFetch(config);

        if (datesToFetch.Count == 0)
        {
            _logger.LogInformation("No new dates to fetch from JobTech Links");
            yield break;
        }

        _logger.LogInformation("Fetching {Count} date files from JobTech Links", datesToFetch.Count);

        // Download all date files in parallel, then yield results in date order
        var downloadResults = await DownloadAllDatesAsync(datesToFetch, cancellationToken);
        var totalJobs = 0;

        foreach (var date in datesToFetch)
        {
            if (!downloadResults.TryGetValue(date, out var jobs) || jobs.Count == 0)
                continue;

            // Enforce per-fetch limit
            if (config.MaxJobsPerFetch > 0 && totalJobs + jobs.Count > config.MaxJobsPerFetch)
            {
                var remaining = config.MaxJobsPerFetch - totalJobs;
                if (remaining <= 0)
                {
                    _logger.LogInformation("Reached max jobs limit of {Limit}, stopping fetch", config.MaxJobsPerFetch);
                    break;
                }
                jobs = jobs.Take(remaining).ToList();
            }

            totalJobs += jobs.Count;
            config.LastDownloadedDate = date.ToString("yyyy-MM-dd");

            _logger.LogInformation("Yielding {Count} jobs from {Date}", jobs.Count, config.LastDownloadedDate);

            yield return new FetchedJobBatch
            {
                Jobs = jobs,
                CheckpointConfig = JsonSerializer.Serialize(config)
            };
        }

        _logger.LogInformation("Total fetched {Count} jobs from JobTech Links", totalJobs);
    }

    private async Task<Dictionary<DateOnly, List<FetchedJob>>> DownloadAllDatesAsync(
        List<DateOnly> dates,
        CancellationToken cancellationToken)
    {
        var results = new ConcurrentDictionary<DateOnly, List<FetchedJob>>();
        using var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);

        var tasks = dates.Select(async date =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var dateStr = date.ToString("yyyy-MM-dd");
                var downloadUrl = $"{BaseDownloadUrl}/{dateStr}.tar.gz";

                _logger.LogInformation("Downloading JobTech Links file for {Date}", dateStr);

                var jobs = await DownloadAndParseAsync(downloadUrl, cancellationToken);
                if (jobs.Count > 0)
                {
                    results[date] = jobs;
                    _logger.LogInformation("Processed {Count} jobs from {Date}", jobs.Count, dateStr);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return new Dictionary<DateOnly, List<FetchedJob>>(results);
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

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // Find and parse JSON files (JSONL format — one JSON object per line)
            foreach (var jsonFile in Directory.GetFiles(extractDir, "*.json", SearchOption.AllDirectories))
            {
                var lineCount = 0;
                var parsedCount = 0;
                var malformedCount = 0;

                await foreach (var line in File.ReadLinesAsync(jsonFile, cancellationToken))
                {
                    lineCount++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var entry = JsonSerializer.Deserialize<JobTechLinksEntry>(line, jsonOptions);
                        if (entry?.OriginalJobPosting != null)
                        {
                            jobs.Add(MapEntryToFetchedJob(entry));
                            parsedCount++;
                        }
                    }
                    catch (JsonException ex)
                    {
                        malformedCount++;
                        if (malformedCount <= 5)
                        {
                            var truncated = line.Length > 200 ? line[..200] + "..." : line;
                            _logger.LogDebug(
                                ex, "Malformed JSON at line {Line} in {File}: {Content}",
                                lineCount, Path.GetFileName(jsonFile), truncated);
                        }
                    }
                }

                _logger.LogInformation(
                    "Parsed {Parsed}/{Total} lines from {File} ({Malformed} malformed)",
                    parsedCount, lineCount, Path.GetFileName(jsonFile), malformedCount);

                // Warn if high failure rate — likely a schema change upstream
                if (lineCount > 0 && malformedCount > lineCount * 0.1)
                {
                    _logger.LogWarning(
                        "High malformed rate ({Malformed}/{Total} = {Rate:P0}) in {File} — possible schema change",
                        malformedCount, lineCount, (double)malformedCount / lineCount, Path.GetFileName(jsonFile));
                }
            }
        }
        finally
        {
            if (Directory.Exists(extractDir))
            {
                Directory.Delete(extractDir, recursive: true);
            }
        }

        return jobs;
    }

    private static FetchedJob MapEntryToFetchedJob(JobTechLinksEntry entry)
    {
        var posting = entry.OriginalJobPosting!;

        DateTime? postedAt = null;
        if (DateOnly.TryParse(posting.DatePosted, out var datePosted))
            postedAt = datePosted.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        DateTime? expiresAt = null;
        if (!string.IsNullOrEmpty(entry.ApplicationDeadline) &&
            DateTime.TryParse(entry.ApplicationDeadline, out var deadline))
            expiresAt = DateTime.SpecifyKind(deadline, DateTimeKind.Utc);
        else if (!string.IsNullOrEmpty(posting.ValidThrough) &&
                 DateOnly.TryParse(posting.ValidThrough, out var validThrough))
            expiresAt = validThrough.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        // Ensure FirstSeen is UTC if present
        DateTime? firstSeen = entry.FirstSeen.HasValue
            ? DateTime.SpecifyKind(entry.FirstSeen.Value, DateTimeKind.Utc)
            : null;

        return new FetchedJob
        {
            ExternalId = entry.Id,
            Title = posting.Title?.Trim() ?? "Untitled",
            Description = posting.Description,
            CompanyName = posting.HiringOrganization?.Name ?? "Unknown",
            CompanyLogoUrl = null,
            CompanyWebsite = posting.HiringOrganization?.Url,
            Location = posting.JobLocation?.AddressLocality,
            WorkLocationType = null,
            EmploymentType = ParseEmploymentType(posting.EmploymentType, null),
            SalaryMin = null,
            SalaryMax = null,
            SalaryCurrency = "SEK",
            SalaryPeriod = null,
            ExternalUrl = posting.Url,
            ApplicationUrl = posting.Url,
            RequiredSkills = GetSkillsFromEnrichments(entry.TextEnrichmentsResults),
            Benefits = null,
            ExperienceYearsMin = null,
            ExperienceYearsMax = null,
            PostedAt = postedAt ?? firstSeen,
            ExpiresAt = expiresAt,
            Tags = ExtractTagsFromEntry(entry)
        };
    }

    private static EmploymentType? ParseEmploymentType(string? employmentType, string? workingHoursType)
    {
        var combined = $"{employmentType} {workingHoursType}".ToLowerInvariant();

        return combined switch
        {
            var t when t.Contains("heltid") || t.Contains("vanlig") => EmploymentType.FullTime,
            var t when t.Contains("deltid") => EmploymentType.PartTime,
            var t when t.Contains("vikariat") || t.Contains("tidsbegr") => EmploymentType.Temporary,
            var t when t.Contains("praktik") => EmploymentType.Internship,
            var t when t.Contains("frilans") || t.Contains("konsult") => EmploymentType.Freelance,
            _ => null
        };
    }

    private static string? GetSkillsFromEnrichments(JobTechLinksEnrichments? enrichments)
    {
        var candidates = enrichments?.EnrichedResult?.EnrichedCandidates;
        if (candidates == null) return null;

        var skills = new List<string>();

        if (candidates.Competencies != null)
        {
            skills.AddRange(candidates.Competencies
                .Where(c => c.Prediction > 0.3 && !string.IsNullOrEmpty(c.ConceptLabel))
                .Select(c => c.ConceptLabel!)
                .Distinct());
        }

        return skills.Count > 0 ? string.Join(", ", skills.Take(20)) : null;
    }

    private static List<string> ExtractTagsFromEntry(JobTechLinksEntry entry)
    {
        var tags = new List<string>();
        var candidates = entry.TextEnrichmentsResults?.EnrichedResult?.EnrichedCandidates;

        // Add occupation labels
        if (candidates?.Occupations != null)
        {
            tags.AddRange(candidates.Occupations
                .Where(o => o.Prediction > 0.5 && !string.IsNullOrEmpty(o.ConceptLabel))
                .Select(o => o.ConceptLabel!)
                .Distinct());
        }

        // Add relevant occupation from posting
        if (!string.IsNullOrEmpty(entry.OriginalJobPosting?.RelevantOccupation?.Name))
            tags.Add(entry.OriginalJobPosting.RelevantOccupation.Name);

        // Add top competencies
        if (candidates?.Competencies != null)
        {
            tags.AddRange(candidates.Competencies
                .Where(c => c.Prediction > 0.5 && !string.IsNullOrEmpty(c.ConceptLabel))
                .Select(c => c.ConceptLabel!)
                .Distinct());
        }

        return tags.Distinct(StringComparer.OrdinalIgnoreCase).Take(30).ToList();
    }
}
