using System.Text.Json;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using Polly;
using Polly.Retry;

namespace JobRecon.Jobs.Services.Fetchers;

public sealed class ArbetsformedlingenFetcher : IJobFetcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArbetsformedlingenFetcher> _logger;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public JobSourceType SourceType => JobSourceType.Arbetsformedlingen;

    public ArbetsformedlingenFetcher(
        HttpClient httpClient,
        ILogger<ArbetsformedlingenFetcher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<List<FetchedJob>> FetchJobsAsync(
        JobSource source,
        CancellationToken cancellationToken = default)
    {
        var jobs = new List<FetchedJob>();
        var baseUrl = source.BaseUrl ?? "https://jobsearch.api.jobtechdev.se";

        try
        {
            // Parse configuration for search parameters
            var config = !string.IsNullOrEmpty(source.Configuration)
                ? JsonSerializer.Deserialize<ArbetsformedlingenConfig>(source.Configuration)
                : new ArbetsformedlingenConfig();

            var offset = 0;
            const int limit = 100;
            var hasMore = true;

            while (hasMore && offset < 1000) // Max 1000 jobs per fetch
            {
                var url = $"{baseUrl}/search?offset={offset}&limit={limit}";

                if (!string.IsNullOrEmpty(config?.Query))
                {
                    url += $"&q={Uri.EscapeDataString(config.Query)}";
                }

                if (!string.IsNullOrEmpty(config?.Region))
                {
                    url += $"&region={Uri.EscapeDataString(config.Region)}";
                }

                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("Accept", "application/json");

                    if (!string.IsNullOrEmpty(source.ApiKey))
                    {
                        request.Headers.Add("api-key", source.ApiKey);
                    }

                    return await _httpClient.SendAsync(request, cancellationToken);
                });

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Arbetsförmedlingen API returned {StatusCode}: {Reason}",
                        response.StatusCode, response.ReasonPhrase);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ArbetsformedlingenResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Hits is null || result.Hits.Count == 0)
                {
                    hasMore = false;
                    continue;
                }

                foreach (var hit in result.Hits)
                {
                    jobs.Add(MapToFetchedJob(hit));
                }

                offset += limit;
                hasMore = offset < result.Total;
            }

            _logger.LogInformation("Fetched {Count} jobs from Arbetsförmedlingen", jobs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching from Arbetsförmedlingen");
            throw;
        }

        return jobs;
    }

    private static FetchedJob MapToFetchedJob(ArbetsformedlingenHit hit)
    {
        return new FetchedJob
        {
            ExternalId = hit.Id,
            Title = hit.Headline ?? "Untitled",
            Description = hit.Description?.Text,
            CompanyName = hit.Employer?.Name ?? "Unknown",
            CompanyLogoUrl = hit.Logo_Url,
            CompanyWebsite = hit.Employer?.Url,
            Location = hit.Workplace_Address?.Municipality ?? hit.Workplace_Address?.Region,
            WorkLocationType = ParseWorkLocationType(hit.Remote_Work),
            EmploymentType = ParseEmploymentType(hit.Employment_Type?.Label),
            SalaryMin = null, // AF doesn't always provide salary info
            SalaryMax = null,
            SalaryCurrency = "SEK",
            ExternalUrl = hit.Webpage_Url,
            ApplicationUrl = hit.Application_Details?.Url ?? hit.Application_Details?.Email,
            RequiredSkills = hit.Must_Have?.Skills != null
                ? string.Join(", ", hit.Must_Have.Skills.Select(s => s.Label))
                : null,
            PostedAt = hit.Publication_Date,
            ExpiresAt = hit.Application_Deadline,
            Tags = ExtractTags(hit)
        };
    }

    private static WorkLocationType? ParseWorkLocationType(bool? remoteWork)
    {
        return remoteWork switch
        {
            true => WorkLocationType.Remote,
            false => WorkLocationType.OnSite,
            _ => null
        };
    }

    private static EmploymentType? ParseEmploymentType(string? type)
    {
        if (string.IsNullOrEmpty(type)) return null;

        return type.ToLower() switch
        {
            var t when t.Contains("heltid") => EmploymentType.FullTime,
            var t when t.Contains("deltid") => EmploymentType.PartTime,
            var t when t.Contains("vikariat") || t.Contains("tidsbegränsad") => EmploymentType.Temporary,
            var t when t.Contains("praktik") => EmploymentType.Internship,
            _ => null
        };
    }

    private static List<string> ExtractTags(ArbetsformedlingenHit hit)
    {
        var tags = new List<string>();

        if (hit.Must_Have?.Skills != null)
        {
            tags.AddRange(hit.Must_Have.Skills.Select(s => s.Label).Where(l => !string.IsNullOrEmpty(l))!);
        }

        if (hit.Nice_To_Have?.Skills != null)
        {
            tags.AddRange(hit.Nice_To_Have.Skills.Select(s => s.Label).Where(l => !string.IsNullOrEmpty(l))!);
        }

        if (!string.IsNullOrEmpty(hit.Occupation?.Label))
        {
            tags.Add(hit.Occupation.Label);
        }

        return tags.Distinct().Take(20).ToList();
    }
}

// Configuration model
public sealed class ArbetsformedlingenConfig
{
    public string? Query { get; set; }
    public string? Region { get; set; }
    public string? OccupationGroup { get; set; }
}

// API Response models
public sealed class ArbetsformedlingenResponse
{
    public int Total { get; set; }
    public List<ArbetsformedlingenHit>? Hits { get; set; }
}

public sealed class ArbetsformedlingenHit
{
    public string Id { get; set; } = null!;
    public string? Headline { get; set; }
    public ArbetsformedlingenDescription? Description { get; set; }
    public ArbetsformedlingenEmployer? Employer { get; set; }
    public string? Logo_Url { get; set; }
    public ArbetsformedlingenAddress? Workplace_Address { get; set; }
    public bool? Remote_Work { get; set; }
    public ArbetsformedlingenLabel? Employment_Type { get; set; }
    public ArbetsformedlingenLabel? Occupation { get; set; }
    public string? Webpage_Url { get; set; }
    public ArbetsformedlingenApplicationDetails? Application_Details { get; set; }
    public ArbetsformedlingenRequirements? Must_Have { get; set; }
    public ArbetsformedlingenRequirements? Nice_To_Have { get; set; }
    public DateTime? Publication_Date { get; set; }
    public DateTime? Application_Deadline { get; set; }
}

public sealed class ArbetsformedlingenDescription
{
    public string? Text { get; set; }
}

public sealed class ArbetsformedlingenEmployer
{
    public string? Name { get; set; }
    public string? Url { get; set; }
}

public sealed class ArbetsformedlingenAddress
{
    public string? Municipality { get; set; }
    public string? Region { get; set; }
}

public sealed class ArbetsformedlingenLabel
{
    public string? Label { get; set; }
}

public sealed class ArbetsformedlingenApplicationDetails
{
    public string? Url { get; set; }
    public string? Email { get; set; }
}

public sealed class ArbetsformedlingenRequirements
{
    public List<ArbetsformedlingenLabel>? Skills { get; set; }
}
