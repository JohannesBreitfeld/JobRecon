using JobRecon.Matching.Contracts;

namespace JobRecon.Matching.Services;

internal sealed class PreprocessedProfile
{
    public required HashSet<string> NormalizedSkills { get; init; }
    public required List<SkillDto> Skills { get; init; }
    public required string? CurrentJobTitleLower { get; init; }
    public required List<(string TitleLower, int Priority)> DesiredJobTitlesLower { get; init; }
    public required List<string> PreferredLocationsLower { get; init; }
    public required string? LocationLower { get; init; }
    public required HashSet<string> PreferredEmploymentTypesLower { get; init; }
    public required HashSet<string> ExcludedCompaniesLower { get; init; }
    public required JobPreferenceDto? Preferences { get; init; }
    public required int? YearsOfExperience { get; init; }

    public static PreprocessedProfile From(ProfileDto profile)
    {
        var prefs = profile.Preferences;

        return new PreprocessedProfile
        {
            NormalizedSkills = profile.Skills
                .Select(s => s.Name.ToLowerInvariant().Trim())
                .ToHashSet(),
            Skills = profile.Skills,
            CurrentJobTitleLower = profile.CurrentJobTitle?.ToLowerInvariant(),
            DesiredJobTitlesLower = profile.DesiredJobTitles
                .Select(dt => (dt.Title.ToLowerInvariant(), dt.Priority))
                .ToList(),
            PreferredLocationsLower = string.IsNullOrEmpty(prefs?.PreferredLocations)
                ? []
                : prefs!.PreferredLocations
                    .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim().ToLowerInvariant())
                    .ToList(),
            LocationLower = profile.Location?.ToLowerInvariant(),
            PreferredEmploymentTypesLower = string.IsNullOrEmpty(prefs?.PreferredEmploymentTypes)
                ? []
                : prefs!.PreferredEmploymentTypes
                    .Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLowerInvariant())
                    .ToHashSet(),
            ExcludedCompaniesLower = string.IsNullOrEmpty(prefs?.ExcludedCompanies)
                ? []
                : prefs!.ExcludedCompanies
                    .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim().ToLowerInvariant())
                    .ToHashSet(),
            Preferences = prefs,
            YearsOfExperience = profile.YearsOfExperience
        };
    }
}
