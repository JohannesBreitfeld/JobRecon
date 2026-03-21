using System.Net.Http.Json;
using JobRecon.Matching.Contracts;

namespace JobRecon.Matching.Services;

public sealed class JobsClient : IJobsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JobsClient> _logger;

    public JobsClient(HttpClient httpClient, ILogger<JobsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<JobListDto?> GetActiveJobsAsync(int limit, int offset, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/jobs?pageSize={limit}&page={offset / limit + 1}&status=Active",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get jobs: {StatusCode}", response.StatusCode);
                return null;
            }

            var jobsResponse = await response.Content.ReadFromJsonAsync<JobsApiResponse>(cancellationToken);
            if (jobsResponse == null) return null;

            return new JobListDto(
                jobsResponse.Jobs.Select(MapToJobDto).ToList(),
                jobsResponse.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs");
            return null;
        }
    }

    public async Task<JobDto?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/jobs/{jobId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get job {JobId}: {StatusCode}", jobId, response.StatusCode);
                return null;
            }

            var jobResponse = await response.Content.ReadFromJsonAsync<JobApiResponse>(cancellationToken);
            if (jobResponse == null) return null;

            return MapToJobDto(jobResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job {JobId}", jobId);
            return null;
        }
    }

    private static JobDto MapToJobDto(JobApiResponse response)
    {
        return new JobDto(
            response.Id,
            response.Title,
            response.Description,
            response.Location,
            response.WorkLocationType,
            response.EmploymentType,
            response.SalaryMin,
            response.SalaryMax,
            response.SalaryCurrency,
            response.RequiredSkills,
            response.ExperienceYearsMin,
            response.ExperienceYearsMax,
            response.PostedAt,
            response.ExternalUrl,
            new CompanyDto(
                response.Company?.Id ?? Guid.Empty,
                response.Company?.Name ?? "Unknown",
                response.Company?.LogoUrl,
                response.Company?.Industry),
            response.Tags ?? []);
    }

    // Internal models matching Jobs API response
    private sealed record JobsApiResponse(
        List<JobApiResponse> Jobs,
        int TotalCount,
        int Page,
        int PageSize);

    private sealed record JobApiResponse(
        Guid Id,
        string Title,
        string? Description,
        string? Location,
        string? WorkLocationType,
        string? EmploymentType,
        decimal? SalaryMin,
        decimal? SalaryMax,
        string? SalaryCurrency,
        string? RequiredSkills,
        int? ExperienceYearsMin,
        int? ExperienceYearsMax,
        DateTime? PostedAt,
        string? ExternalUrl,
        CompanyApiResponse? Company,
        List<string>? Tags);

    private sealed record CompanyApiResponse(
        Guid Id,
        string Name,
        string? LogoUrl,
        string? Industry);
}
