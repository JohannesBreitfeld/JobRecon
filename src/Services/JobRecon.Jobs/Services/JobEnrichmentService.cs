using System.Collections.Concurrent;
using AngleSharp;
using AngleSharp.Html.Parser;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using AngleSharpConfig = AngleSharp.Configuration;

namespace JobRecon.Jobs.Services;

public sealed class JobEnrichmentService : IJobEnrichmentService
{
    private readonly JobsDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly ILogger<JobEnrichmentService> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly ConcurrentDictionary<string, DateTime> _domainLastAccess = new();
    private static readonly TimeSpan RateLimitDelay = TimeSpan.FromSeconds(1);

    public JobEnrichmentService(
        JobsDbContext dbContext,
        HttpClient httpClient,
        ILogger<JobEnrichmentService> logger)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _logger = logger;

        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
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

        _logger.LogInformation("Enriching {Count} pending jobs", pendingJobs.Count);

        var enrichedCount = 0;

        foreach (var job in pendingJobs)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await EnrichJobInternalAsync(job, cancellationToken);
                enrichedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enrich job {JobId}", job.Id);
            }
        }

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

            var htmlContent = await FetchHtmlAsync(job.ExternalUrl!, cancellationToken);

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

    private async Task<string?> FetchHtmlAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "JobRecon/1.0 (Job aggregation service)");
                request.Headers.Add("Accept", "text/html,application/xhtml+xml");

                return await _httpClient.SendAsync(request, cancellationToken);
            });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Failed to fetch {Url}: {StatusCode}", url, response.StatusCode);
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error fetching {Url}", url);
            return null;
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

    private static string CleanText(string text)
    {
        // Remove excessive whitespace
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
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
    }
}
