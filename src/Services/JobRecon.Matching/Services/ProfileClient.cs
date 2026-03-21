using System.Net.Http.Json;
using JobRecon.Matching.Contracts;

namespace JobRecon.Matching.Services;

public sealed class ProfileClient : IProfileClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfileClient> _logger;

    public ProfileClient(HttpClient httpClient, ILogger<ProfileClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/profile/{userId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get profile for user {UserId}: {StatusCode}",
                    userId, response.StatusCode);
                return null;
            }

            var profileResponse = await response.Content.ReadFromJsonAsync<ProfileApiResponse>(cancellationToken);
            if (profileResponse == null) return null;

            return MapToProfileDto(profileResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile for user {UserId}", userId);
            return null;
        }
    }

    private static ProfileDto MapToProfileDto(ProfileApiResponse response)
    {
        return new ProfileDto(
            response.UserId,
            response.CurrentJobTitle,
            response.Summary,
            response.Location,
            response.YearsOfExperience,
            response.Skills?.Select(s => new SkillDto(s.Name, s.Level, s.YearsOfExperience)).ToList() ?? [],
            response.DesiredJobTitles?.Select(t => new DesiredJobTitleDto(t.Title, t.Priority)).ToList() ?? [],
            response.Preferences != null ? new JobPreferenceDto(
                response.Preferences.MinSalary,
                response.Preferences.MaxSalary,
                response.Preferences.PreferredLocations,
                response.Preferences.IsRemotePreferred,
                response.Preferences.IsHybridAccepted,
                response.Preferences.IsOnSiteAccepted,
                response.Preferences.PreferredEmploymentTypes,
                response.Preferences.PreferredIndustries,
                response.Preferences.ExcludedCompanies,
                response.Preferences.IsActivelyLooking) : null);
    }

    // Internal models matching Profile API response
    private sealed record ProfileApiResponse(
        Guid UserId,
        string? CurrentJobTitle,
        string? Summary,
        string? Location,
        int? YearsOfExperience,
        List<SkillApiResponse>? Skills,
        List<DesiredJobTitleApiResponse>? DesiredJobTitles,
        JobPreferenceApiResponse? Preferences);

    private sealed record SkillApiResponse(
        string Name,
        string Level,
        int? YearsOfExperience);

    private sealed record DesiredJobTitleApiResponse(
        string Title,
        int Priority);

    private sealed record JobPreferenceApiResponse(
        decimal? MinSalary,
        decimal? MaxSalary,
        string? PreferredLocations,
        bool IsRemotePreferred,
        bool IsHybridAccepted,
        bool IsOnSiteAccepted,
        string? PreferredEmploymentTypes,
        string? PreferredIndustries,
        string? ExcludedCompanies,
        bool IsActivelyLooking);
}
