using JobRecon.Jobs.Contracts;

namespace JobRecon.Jobs.Endpoints;

public static class JobSourceEndpoints
{
    public static void MapJobSourceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/jobs/sources")
            .WithTags("Job Sources")
            .RequireAuthorization();

        group.MapGet("/", GetJobSources)
            .WithName("GetJobSources")
            .WithSummary("Get all job sources");

        group.MapGet("/{id:guid}", GetJobSource)
            .WithName("GetJobSource")
            .WithSummary("Get job source details");

        group.MapPost("/", CreateJobSource)
            .WithName("CreateJobSource")
            .WithSummary("Create a new job source");

        group.MapPut("/{id:guid}", UpdateJobSource)
            .WithName("UpdateJobSource")
            .WithSummary("Update job source");

        group.MapDelete("/{id:guid}", DeleteJobSource)
            .WithName("DeleteJobSource")
            .WithSummary("Delete job source");

        group.MapPost("/{id:guid}/fetch", TriggerFetch)
            .WithName("TriggerFetch")
            .WithSummary("Trigger manual job fetch");
    }

    private static async Task<IResult> GetJobSources(
        IJobSourceService jobSourceService,
        CancellationToken cancellationToken)
    {
        var result = await jobSourceService.GetJobSourcesAsync(cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Error);
    }

    private static async Task<IResult> GetJobSource(
        Guid id,
        IJobSourceService jobSourceService,
        CancellationToken cancellationToken)
    {
        var result = await jobSourceService.GetJobSourceAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error);
    }

    private static async Task<IResult> CreateJobSource(
        CreateJobSourceRequest request,
        IJobSourceService jobSourceService,
        CancellationToken cancellationToken)
    {
        var result = await jobSourceService.CreateJobSourceAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/jobs/sources/{result.Value.Id}", result.Value)
            : Results.Conflict(result.Error);
    }

    private static async Task<IResult> UpdateJobSource(
        Guid id,
        UpdateJobSourceRequest request,
        IJobSourceService jobSourceService,
        CancellationToken cancellationToken)
    {
        var result = await jobSourceService.UpdateJobSourceAsync(id, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.Code == "JobSource.NotFound"
                ? Results.NotFound(result.Error)
                : Results.Conflict(result.Error);
    }

    private static async Task<IResult> DeleteJobSource(
        Guid id,
        IJobSourceService jobSourceService,
        CancellationToken cancellationToken)
    {
        var result = await jobSourceService.DeleteJobSourceAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.Code == "JobSource.NotFound"
                ? Results.NotFound(result.Error)
                : Results.BadRequest(result.Error);
    }

    private static async Task<IResult> TriggerFetch(
        Guid id,
        IJobSourceService jobSourceService,
        CancellationToken cancellationToken)
    {
        var result = await jobSourceService.TriggerFetchAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Accepted()
            : result.Error.Code == "JobSource.NotFound"
                ? Results.NotFound(result.Error)
                : Results.BadRequest(result.Error);
    }
}
