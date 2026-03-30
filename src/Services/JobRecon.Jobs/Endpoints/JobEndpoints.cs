using System.Security.Claims;
using JobRecon.Jobs.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace JobRecon.Jobs.Endpoints;

public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/jobs")
            .WithTags("Jobs");

        group.MapGet("/", SearchJobs)
            .WithName("SearchJobs")
            .WithSummary("Search jobs with filters");

        group.MapGet("/{id:guid}", GetJob)
            .WithName("GetJob")
            .WithSummary("Get job details");

        group.MapPost("/", CreateJob)
            .WithName("CreateJob")
            .WithSummary("Create a manual job listing")
            .RequireAuthorization();

        group.MapGet("/statistics", GetStatistics)
            .WithName("GetJobStatistics")
            .WithSummary("Get job statistics");

        group.MapGet("/tags", GetTags)
            .WithName("GetTags")
            .WithSummary("Search distinct job tags");

        group.MapGet("/companies", GetCompanies)
            .WithName("GetCompanies")
            .WithSummary("Search companies");

        group.MapGet("/companies/{id:guid}", GetCompany)
            .WithName("GetCompany")
            .WithSummary("Get company details");

        // Saved jobs
        var savedGroup = app.MapGroup("/api/jobs/saved")
            .WithTags("Saved Jobs")
            .RequireAuthorization();

        savedGroup.MapGet("/", GetSavedJobs)
            .WithName("GetSavedJobs")
            .WithSummary("Get user's saved jobs");

        savedGroup.MapPost("/{jobId:guid}", SaveJob)
            .WithName("SaveJob")
            .WithSummary("Save a job");

        savedGroup.MapPut("/{jobId:guid}", UpdateSavedJob)
            .WithName("UpdateSavedJob")
            .WithSummary("Update saved job status/notes");

        savedGroup.MapDelete("/{jobId:guid}", RemoveSavedJob)
            .WithName("RemoveSavedJob")
            .WithSummary("Remove saved job");
    }

    private static async Task<IResult> SearchJobs(
        [AsParameters] JobSearchRequest request,
        IJobService jobService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetOptionalUserId(user);

        var result = await jobService.SearchJobsAsync(userId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Error);
    }

    private static async Task<IResult> GetJob(
        Guid id,
        IJobService jobService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetOptionalUserId(user);

        var result = await jobService.GetJobAsync(id, userId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error);
    }

    private static async Task<IResult> CreateJob(
        CreateJobRequest request,
        IJobService jobService,
        CancellationToken cancellationToken)
    {
        var result = await jobService.CreateJobAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/jobs/{result.Value.Id}", result.Value)
            : Results.BadRequest(result.Error);
    }

    private static async Task<IResult> GetStatistics(
        IJobService jobService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetOptionalUserId(user);

        var result = await jobService.GetStatisticsAsync(userId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Error);
    }

    private static async Task<IResult> GetTags(
        [FromQuery] string? search,
        [FromQuery] int limit,
        IJobService jobService,
        CancellationToken cancellationToken)
    {
        var result = await jobService.GetTagsAsync(search, limit > 0 ? limit : 50, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Error);
    }

    private static async Task<IResult> GetCompanies(
        [FromQuery] string? search,
        [FromQuery] int limit,
        IJobService jobService,
        CancellationToken cancellationToken)
    {
        var result = await jobService.GetCompaniesAsync(search, limit > 0 ? limit : 20, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Error);
    }

    private static async Task<IResult> GetCompany(
        Guid id,
        IJobService jobService,
        CancellationToken cancellationToken)
    {
        var result = await jobService.GetCompanyAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error);
    }

    private static async Task<IResult> GetSavedJobs(
        IJobService jobService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequiredUserId(user, out var userId))
            return Results.Unauthorized();

        var result = await jobService.GetSavedJobsAsync(userId, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Error);
    }

    private static async Task<IResult> SaveJob(
        Guid jobId,
        SaveJobRequest request,
        IJobService jobService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequiredUserId(user, out var userId))
            return Results.Unauthorized();

        var result = await jobService.SaveJobAsync(userId, jobId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/jobs/saved/{jobId}", result.Value)
            : result.Error.Code == "Job.NotFound"
                ? Results.NotFound(result.Error)
                : Results.Conflict(result.Error);
    }

    private static async Task<IResult> UpdateSavedJob(
        Guid jobId,
        UpdateSavedJobRequest request,
        IJobService jobService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequiredUserId(user, out var userId))
            return Results.Unauthorized();

        var result = await jobService.UpdateSavedJobAsync(userId, jobId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error);
    }

    private static async Task<IResult> RemoveSavedJob(
        Guid jobId,
        IJobService jobService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequiredUserId(user, out var userId))
            return Results.Unauthorized();

        var result = await jobService.RemoveSavedJobAsync(userId, jobId, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(result.Error);
    }

    private static Guid? GetOptionalUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static bool TryGetRequiredUserId(ClaimsPrincipal user, out Guid userId)
        => Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
}
