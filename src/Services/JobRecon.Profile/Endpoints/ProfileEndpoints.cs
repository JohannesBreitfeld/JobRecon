using System.Security.Claims;
using JobRecon.Profile.Contracts;
using JobRecon.Profile.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobRecon.Profile.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/profile")
            .RequireAuthorization()
            .WithTags("Profile");

        group.MapGet("/", GetProfile)
            .WithName("GetProfile")
            .WithDescription("Get the current user's profile");

        group.MapPost("/", CreateProfile)
            .WithName("CreateProfile")
            .WithDescription("Create a new profile for the current user");

        group.MapPut("/", UpdateProfile)
            .WithName("UpdateProfile")
            .WithDescription("Update the current user's profile");

        group.MapPost("/skills", AddSkill)
            .WithName("AddSkill")
            .WithDescription("Add a skill to the current user's profile");

        group.MapDelete("/skills/{skillId:guid}", RemoveSkill)
            .WithName("RemoveSkill")
            .WithDescription("Remove a skill from the current user's profile");

        group.MapGet("/preferences", GetPreferences)
            .WithName("GetPreferences")
            .WithDescription("Get the current user's job preferences");

        group.MapPut("/preferences", UpdatePreferences)
            .WithName("UpdatePreferences")
            .WithDescription("Update the current user's job preferences");

        group.MapPost("/cv", UploadCV)
            .WithName("UploadCV")
            .WithDescription("Upload a CV document")
            .DisableAntiforgery();

        group.MapGet("/cv/{documentId:guid}", DownloadCV)
            .WithName("DownloadCV")
            .WithDescription("Download a CV document");

        group.MapDelete("/cv/{documentId:guid}", DeleteCV)
            .WithName("DeleteCV")
            .WithDescription("Delete a CV document");

        group.MapPost("/cv/{documentId:guid}/primary", SetPrimaryCV)
            .WithName("SetPrimaryCV")
            .WithDescription("Set a CV document as the primary CV");

        return endpoints;
    }

    private static async Task<IResult> GetProfile(
        ClaimsPrincipal user,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await profileService.GetProfileAsync(userId.Value, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error });
    }

    private static async Task<IResult> CreateProfile(
        ClaimsPrincipal user,
        CreateProfileRequest request,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await profileService.CreateProfileAsync(userId.Value, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/profile", result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> UpdateProfile(
        ClaimsPrincipal user,
        UpdateProfileRequest request,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await profileService.UpdateProfileAsync(userId.Value, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> AddSkill(
        ClaimsPrincipal user,
        AddSkillRequest request,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await profileService.AddSkillAsync(userId.Value, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/profile/skills/{result.Value!.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> RemoveSkill(
        Guid skillId,
        ClaimsPrincipal user,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await profileService.RemoveSkillAsync(userId.Value, skillId, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetPreferences(
        ClaimsPrincipal user,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await profileService.GetPreferencesAsync(userId.Value, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { error = result.Error });
    }

    private static async Task<IResult> UpdatePreferences(
        ClaimsPrincipal user,
        UpdateJobPreferenceRequest request,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await profileService.UpdatePreferencesAsync(userId.Value, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> UploadCV(
        ClaimsPrincipal user,
        IFormFile file,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        if (file.Length == 0)
        {
            return Results.BadRequest(new { error = "File is empty" });
        }

        const long maxFileSize = 10 * 1024 * 1024; // 10 MB
        if (file.Length > maxFileSize)
        {
            return Results.BadRequest(new { error = "File size exceeds the 10 MB limit." });
        }

        var allowedTypes = new[] { "application/pdf", "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

        if (!allowedTypes.Contains(file.ContentType))
        {
            return Results.BadRequest(new { error = "Invalid file type. Only PDF and Word documents are allowed." });
        }

        using var stream = file.OpenReadStream();
        var result = await profileService.UploadCVAsync(
            userId.Value,
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/profile/cv/{result.Value!.Id}", result.Value)
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> DownloadCV(
        Guid documentId,
        ClaimsPrincipal user,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await profileService.DownloadCVAsync(userId.Value, documentId, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.NotFound(new { error = result.Error });
        }

        var (fileStream, fileName, contentType) = result.Value;
        return Results.File(fileStream, contentType, fileName);
    }

    private static async Task<IResult> DeleteCV(
        Guid documentId,
        ClaimsPrincipal user,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await profileService.DeleteCVAsync(userId.Value, documentId, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> SetPrimaryCV(
        Guid documentId,
        ClaimsPrincipal user,
        IProfileService profileService,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null) return Results.Unauthorized();

        var result = await profileService.SetPrimaryCVAsync(userId.Value, documentId, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { error = result.Error });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
