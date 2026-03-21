using JobRecon.Matching.Contracts;
using JobRecon.Protos.Profile;

namespace JobRecon.Matching.Services;

public sealed class ProfileClient(
    ProfileGrpc.ProfileGrpcClient grpcClient,
    ILogger<ProfileClient> logger) : IProfileClient
{
    public async Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await grpcClient.GetProfileAsync(
                new GetProfileRequest { UserId = userId.ToString() },
                cancellationToken: cancellationToken);

            return MapToProfileDto(response);
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            logger.LogWarning("Profile not found for user {UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting profile for user {UserId} via gRPC", userId);
            return null;
        }
    }

    private static ProfileDto MapToProfileDto(ProfileResponse response)
    {
        return new ProfileDto(
            Guid.Parse(response.UserId),
            response.HasCurrentJobTitle ? response.CurrentJobTitle : null,
            response.HasSummary ? response.Summary : null,
            response.HasLocation ? response.Location : null,
            response.HasYearsOfExperience ? response.YearsOfExperience : null,
            response.Skills.Select(s => new SkillDto(
                s.Name,
                s.Level,
                s.HasYearsOfExperience ? s.YearsOfExperience : null)).ToList(),
            response.DesiredJobTitles.Select(t => new DesiredJobTitleDto(
                t.Title,
                t.Priority)).ToList(),
            response.Preferences is not null ? MapPreferences(response.Preferences) : null);
    }

    private static JobPreferenceDto MapPreferences(JobPreferenceMessage pref)
    {
        return new JobPreferenceDto(
            pref.HasMinSalary ? (decimal)pref.MinSalary : null,
            pref.HasMaxSalary ? (decimal)pref.MaxSalary : null,
            pref.HasPreferredLocations ? pref.PreferredLocations : null,
            pref.IsRemotePreferred,
            pref.IsHybridAccepted,
            pref.IsOnSiteAccepted,
            pref.HasPreferredEmploymentTypes ? pref.PreferredEmploymentTypes : null,
            pref.HasPreferredIndustries ? pref.PreferredIndustries : null,
            pref.HasExcludedCompanies ? pref.ExcludedCompanies : null,
            pref.IsActivelyLooking);
    }
}
