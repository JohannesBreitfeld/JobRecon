using Grpc.Core;
using JobRecon.Profile.Infrastructure;
using JobRecon.Protos.Profile;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Profile.Grpc;

public sealed class ProfileGrpcService(
    ProfileDbContext dbContext,
    ILogger<ProfileGrpcService> logger) : ProfileGrpc.ProfileGrpcBase
{
    public override async Task<ProfileResponse> GetProfile(
        GetProfileRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format."));
        }

        var profile = await dbContext.UserProfiles
            .Include(p => p.Skills)
            .Include(p => p.DesiredJobTitles)
            .Include(p => p.JobPreference)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, context.CancellationToken);

        if (profile is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Profile not found for user {userId}."));
        }

        logger.LogDebug("Returning profile for user {UserId} via gRPC", userId);

        var response = new ProfileResponse
        {
            UserId = profile.UserId.ToString(),
            CurrentJobTitle = profile.CurrentJobTitle ?? "",
            Summary = profile.Summary ?? "",
            Location = profile.Location ?? ""
        };

        if (profile.YearsOfExperience.HasValue)
            response.YearsOfExperience = profile.YearsOfExperience.Value;

        foreach (var skill in profile.Skills)
        {
            var msg = new SkillMessage
            {
                Name = skill.Name,
                Level = skill.Level.ToString()
            };
            if (skill.YearsOfExperience.HasValue)
                msg.YearsOfExperience = skill.YearsOfExperience.Value;

            response.Skills.Add(msg);
        }

        foreach (var title in profile.DesiredJobTitles)
        {
            response.DesiredJobTitles.Add(new DesiredJobTitleMessage
            {
                Title = title.Title,
                Priority = title.Priority
            });
        }

        if (profile.JobPreference is { } pref)
        {
            var prefMsg = new JobPreferenceMessage
            {
                IsRemotePreferred = pref.IsRemotePreferred,
                IsHybridAccepted = pref.IsHybridAccepted,
                IsOnSiteAccepted = pref.IsOnSiteAccepted,
                IsActivelyLooking = pref.IsActivelyLooking,
                PreferredLocations = pref.PreferredLocations ?? "",
                PreferredEmploymentTypes = pref.PreferredEmploymentType.ToString(),
                PreferredIndustries = pref.PreferredIndustries ?? "",
                ExcludedCompanies = pref.ExcludedCompanies ?? ""
            };

            if (pref.MinSalary.HasValue)
                prefMsg.MinSalary = pref.MinSalary.Value;
            if (pref.MaxSalary.HasValue)
                prefMsg.MaxSalary = pref.MaxSalary.Value;

            response.Preferences = prefMsg;
        }

        return response;
    }
}
