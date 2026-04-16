using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Html.Parser;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using AngleSharpConfig = AngleSharp.Configuration;

namespace JobRecon.Jobs.Services;

public sealed class JobEnrichmentService : IJobEnrichmentService
{
    private readonly JobsDbContext _dbContext;
    private readonly IPlaywrightPageFactory _pageFactory;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<JobEnrichmentService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _domainLastAccess = new();
    private static readonly TimeSpan RateLimitDelay = TimeSpan.FromSeconds(1);
    private const int MaxConcurrency = 5;

    private static readonly string[] DeadlineLabels =
    [
        "sista ansökningsdag", "sista ansökningsdatum", "ansök senast", "sista dag att ansöka",
        "apply before", "application deadline", "closing date", "last day to apply"
    ];

    private static readonly Regex DatePatternIso = new(
        @"\b(\d{4}-\d{2}-\d{2})\b", RegexOptions.Compiled);

    private static readonly Regex DatePatternDmy = new(
        @"\b(\d{1,2})\s+(januari|februari|mars|april|maj|juni|juli|augusti|september|oktober|november|december|january|february|march|april|may|june|july|august|september|october|november|december)\s+(\d{4})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public JobEnrichmentService(
        JobsDbContext dbContext,
        IPlaywrightPageFactory pageFactory,
        IGeocodingService geocodingService,
        ILogger<JobEnrichmentService> logger)
    {
        _dbContext = dbContext;
        _pageFactory = pageFactory;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task EnrichJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs.FindAsync([jobId], cancellationToken);

        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found for enrichment", jobId);
            return;
        }

        if (string.IsNullOrEmpty(job.ExternalUrl))
        {
            _logger.LogDebug("Job {JobId} has no external URL to enrich from", jobId);
            job.IsEnriched = true;
            job.EnrichedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await EnrichJobInternalAsync(job, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> EnrichPendingJobsAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        var pendingJobs = await _dbContext.Jobs
            .Where(j => !j.IsEnriched &&
                        j.ExternalUrl != null &&
                        j.Status == JobStatus.Active &&
                        (j.EnrichmentError == null || j.UpdatedAt < DateTime.UtcNow.AddHours(-24)))
            .OrderBy(j => j.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pendingJobs.Count == 0)
        {
            _logger.LogDebug("No pending jobs to enrich");
            return 0;
        }

        _logger.LogInformation("Enriching {Count} pending jobs (concurrency: {Concurrency})",
            pendingJobs.Count, MaxConcurrency);

        var enrichedCount = 0;

        await Parallel.ForEachAsync(pendingJobs,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxConcurrency,
                CancellationToken = cancellationToken
            },
            async (job, ct) =>
            {
                try
                {
                    await EnrichJobInternalAsync(job, ct);
                    Interlocked.Increment(ref enrichedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to enrich job {JobId}", job.Id);
                }
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully enriched {Count} jobs", enrichedCount);
        return enrichedCount;
    }

    private async Task EnrichJobInternalAsync(Job job, CancellationToken cancellationToken)
    {
        try
        {
            // Rate limiting per domain
            await RateLimitAsync(job.ExternalUrl!, cancellationToken);

            var htmlContent = await FetchRenderedHtmlAsync(job.ExternalUrl!, cancellationToken);

            if (string.IsNullOrEmpty(htmlContent))
            {
                job.EnrichmentError = "Failed to fetch content";
                job.UpdatedAt = DateTime.UtcNow;
                return;
            }

            var enrichedData = await ParseJobContentAsync(htmlContent, cancellationToken);

            // Only update if we got better data
            if (!string.IsNullOrEmpty(enrichedData.Description) &&
                (string.IsNullOrEmpty(job.Description) || enrichedData.Description.Length > job.Description.Length))
            {
                job.Description = enrichedData.Description;
            }

            if (!string.IsNullOrEmpty(enrichedData.RequiredSkills) && string.IsNullOrEmpty(job.RequiredSkills))
            {
                job.RequiredSkills = enrichedData.RequiredSkills;
            }

            if (!string.IsNullOrEmpty(enrichedData.Benefits) && string.IsNullOrEmpty(job.Benefits))
            {
                job.Benefits = enrichedData.Benefits;
            }

            // Only set ExpiresAt if not already set from JobTech
            if (job.ExpiresAt is null && enrichedData.ExpiresAt is not null)
            {
                job.ExpiresAt = enrichedData.ExpiresAt;
                _logger.LogDebug("Extracted application deadline {Deadline} for job {JobId}",
                    enrichedData.ExpiresAt, job.Id);
            }

            // Geocode location if not already geocoded
            if (job.LocalityId is null && !string.IsNullOrEmpty(job.Location))
            {
                var geoResult = await _geocodingService.GeocodeAsync(job.Location, cancellationToken);
                if (geoResult is not null)
                {
                    job.LocalityId = geoResult.GeoNameId;
                    job.Latitude = geoResult.Latitude;
                    job.Longitude = geoResult.Longitude;
                }
            }

            job.IsEnriched = true;
            job.EnrichedAt = DateTime.UtcNow;
            job.EnrichmentError = null;
            job.UpdatedAt = DateTime.UtcNow;

            _logger.LogDebug("Enriched job {JobId} from {Url}", job.Id, job.ExternalUrl);
        }
        catch (Exception ex)
        {
            job.EnrichmentError = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            job.UpdatedAt = DateTime.UtcNow;
            _logger.LogWarning(ex, "Error enriching job {JobId} from {Url}", job.Id, job.ExternalUrl);
        }
    }

    private async Task RateLimitAsync(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return;

        var domain = uri.Host;

        if (_domainLastAccess.TryGetValue(domain, out var lastAccess))
        {
            var timeSinceLastAccess = DateTime.UtcNow - lastAccess;
            if (timeSinceLastAccess < RateLimitDelay)
            {
                var delay = RateLimitDelay - timeSinceLastAccess;
                await Task.Delay(delay, cancellationToken);
            }
        }

        _domainLastAccess[domain] = DateTime.UtcNow;
    }

    private async Task<string?> FetchRenderedHtmlAsync(string url, CancellationToken cancellationToken)
    {
        IPage? page = null;
        try
        {
            page = await _pageFactory.CreatePageAsync();

            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                Timeout = 15_000,
                WaitUntil = WaitUntilState.NetworkIdle
            });

            if (response is null || !response.Ok)
            {
                _logger.LogDebug("Failed to navigate to {Url}: {Status}",
                    url, response?.Status ?? 0);
                return null;
            }

            return await page.ContentAsync();
        }
        catch (TimeoutException)
        {
            // NetworkIdle timed out — try to get whatever content loaded
            if (page is not null)
            {
                try
                {
                    return await page.ContentAsync();
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error fetching {Url} with Playwright", url);
            return null;
        }
        finally
        {
            if (page is not null)
            {
                var context = page.Context;
                await page.CloseAsync();
                await context.DisposeAsync();
            }
        }
    }

    private static async Task<EnrichedJobData> ParseJobContentAsync(string html, CancellationToken cancellationToken)
    {
        var result = new EnrichedJobData();

        try
        {
            var config = AngleSharpConfig.Default;
            var context = BrowsingContext.New(config);
            var parser = context.GetService<IHtmlParser>();

            if (parser == null)
                return result;

            var document = await parser.ParseDocumentAsync(html, cancellationToken);

            // --- Extract application deadline ---

            // Strategy A: JSON-LD structured data (most reliable)
            result.ExpiresAt = ExtractDeadlineFromJsonLd(document);

            // Strategy B: HTML text pattern matching (fallback)
            if (result.ExpiresAt is null)
            {
                var bodyText = document.Body?.TextContent;
                if (!string.IsNullOrEmpty(bodyText))
                    result.ExpiresAt = ExtractDeadlineFromText(bodyText);
            }

            // --- Extract job content ---

            // Try common job description selectors
            var descriptionSelectors = new[]
            {
                "[class*='job-description']",
                "[class*='jobDescription']",
                "[class*='description']",
                "[id*='job-description']",
                "[id*='jobDescription']",
                "article",
                ".job-details",
                ".job-content",
                ".posting-content",
                "main"
            };

            foreach (var selector in descriptionSelectors)
            {
                var element = document.QuerySelector(selector);
                if (element != null)
                {
                    var text = element.TextContent?.Trim();
                    if (!string.IsNullOrEmpty(text) && text.Length > 100)
                    {
                        result.Description = CleanText(text);
                        break;
                    }
                }
            }

            // Try to extract requirements/skills
            var requirementsSelectors = new[]
            {
                "[class*='requirement']",
                "[class*='qualification']",
                "[class*='skill']",
                "ul[class*='requirement']",
                "ul[class*='qualification']"
            };

            foreach (var selector in requirementsSelectors)
            {
                var element = document.QuerySelector(selector);
                if (element != null)
                {
                    var text = element.TextContent?.Trim();
                    if (!string.IsNullOrEmpty(text) && text.Length > 20)
                    {
                        result.RequiredSkills = CleanText(text);
                        break;
                    }
                }
            }

            // Try to extract benefits
            var benefitsSelectors = new[]
            {
                "[class*='benefit']",
                "[class*='perk']",
                "[class*='offer']"
            };

            foreach (var selector in benefitsSelectors)
            {
                var element = document.QuerySelector(selector);
                if (element != null)
                {
                    var text = element.TextContent?.Trim();
                    if (!string.IsNullOrEmpty(text) && text.Length > 20)
                    {
                        result.Benefits = CleanText(text);
                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
            // HTML parsing is best-effort; return whatever we collected so far
        }

        return result;
    }

    private static DateTime? ExtractDeadlineFromJsonLd(AngleSharp.Dom.IDocument document)
    {
        var scripts = document.QuerySelectorAll("script[type='application/ld+json']");

        foreach (var script in scripts)
        {
            var json = script.TextContent?.Trim();
            if (string.IsNullOrEmpty(json))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Handle both single objects and arrays
                var elements = root.ValueKind == JsonValueKind.Array
                    ? root.EnumerateArray().ToList()
                    : [root];

                foreach (var element in elements)
                {
                    if (!IsJobPosting(element))
                        continue;

                    if (element.TryGetProperty("validThrough", out var validThrough))
                    {
                        var dateStr = validThrough.GetString();
                        if (TryParseDate(dateStr, out var date))
                            return date;
                    }

                    if (element.TryGetProperty("applicationDeadline", out var deadline))
                    {
                        var dateStr = deadline.GetString();
                        if (TryParseDate(dateStr, out var date))
                            return date;
                    }
                }
            }
            catch (JsonException)
            {
                // Malformed JSON-LD, skip
            }
        }

        return null;
    }

    private static bool IsJobPosting(JsonElement element)
    {
        if (element.TryGetProperty("@type", out var type))
        {
            var typeStr = type.GetString();
            return typeStr is "JobPosting" or "jobPosting";
        }
        return false;
    }

    private static DateTime? ExtractDeadlineFromText(string text)
    {
        var lowerText = text.ToLowerInvariant();

        foreach (var label in DeadlineLabels)
        {
            var labelIndex = lowerText.IndexOf(label, StringComparison.Ordinal);
            if (labelIndex < 0)
                continue;

            // Look at the ~100 characters after the label for a date
            var searchStart = labelIndex + label.Length;
            var searchEnd = Math.Min(searchStart + 100, text.Length);
            var snippet = text[searchStart..searchEnd];

            // Try ISO date format (YYYY-MM-DD)
            var isoMatch = DatePatternIso.Match(snippet);
            if (isoMatch.Success && TryParseDate(isoMatch.Groups[1].Value, out var isoDate))
                return isoDate;

            // Try "D Month YYYY" format (Swedish/English month names)
            var dmyMatch = DatePatternDmy.Match(snippet);
            if (dmyMatch.Success)
            {
                var dateStr = dmyMatch.Value;
                if (TryParseDate(dateStr, out var dmyDate))
                    return dmyDate;
            }
        }

        return null;
    }

    private static bool TryParseDate(string? dateStr, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(dateStr))
            return false;

        // Try standard ISO formats
        if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result))
            return true;

        // Try Swedish culture
        if (DateTime.TryParse(dateStr, new CultureInfo("sv-SE"),
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result))
            return true;

        return false;
    }

    private static string CleanText(string text)
    {
        // Remove excessive whitespace
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Trim();

        // Limit length
        if (text.Length > 50000)
        {
            text = text[..50000];
        }

        return text;
    }

    private sealed class EnrichedJobData
    {
        public string? Description { get; set; }
        public string? RequiredSkills { get; set; }
        public string? Benefits { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
