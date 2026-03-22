using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using JobRecon.Protos.Jobs;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Jobs.Grpc;

public sealed class JobsGrpcService(
    JobsDbContext dbContext,
    ILogger<JobsGrpcService> logger) : JobsGrpc.JobsGrpcBase
{
    public override async Task<JobListResponse> GetActiveJobs(
        GetActiveJobsRequest request,
        ServerCallContext context)
    {
        var query = dbContext.Jobs
            .Include(j => j.Company)
            .Include(j => j.Tags)
            .Where(j => j.Status == JobStatus.Active)
            .OrderByDescending(j => j.PostedAt)
            .AsNoTracking();

        var totalCount = await query.CountAsync(context.CancellationToken);

        var jobs = await query
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(context.CancellationToken);

        logger.LogDebug("Returning {Count}/{Total} active jobs via gRPC (offset={Offset}, limit={Limit})",
            jobs.Count, totalCount, request.Offset, request.Limit);

        var response = new JobListResponse { TotalCount = totalCount };
        foreach (var job in jobs)
        {
            response.Jobs.Add(MapToMessage(job));
        }

        return response;
    }

    public override async Task<JobResponse> GetJob(
        GetJobRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.JobId, out var jobId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid job ID format."));
        }

        var job = await dbContext.Jobs
            .Include(j => j.Company)
            .Include(j => j.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, context.CancellationToken);

        if (job is null)
        {
            return new JobResponse();
        }

        return new JobResponse { Job = MapToMessage(job) };
    }

    private static JobMessage MapToMessage(Job job)
    {
        var msg = new JobMessage
        {
            Id = job.Id.ToString(),
            Title = job.Title ?? "",
            Description = job.Description ?? "",
            Location = job.Location ?? "",
            WorkLocationType = job.WorkLocationType?.ToString() ?? "",
            EmploymentType = job.EmploymentType?.ToString() ?? "",
            SalaryCurrency = job.SalaryCurrency ?? "",
            RequiredSkills = job.RequiredSkills ?? "",
            ExternalUrl = job.ExternalUrl ?? "",
            Company = new CompanyMessage
            {
                Id = job.Company.Id.ToString(),
                Name = job.Company.Name ?? "",
                LogoUrl = job.Company.LogoUrl ?? "",
                Industry = job.Company.Industry ?? ""
            }
        };

        if (job.SalaryMin.HasValue)
            msg.SalaryMin = (double)job.SalaryMin.Value;
        if (job.SalaryMax.HasValue)
            msg.SalaryMax = (double)job.SalaryMax.Value;
        if (job.ExperienceYearsMin.HasValue)
            msg.ExperienceYearsMin = job.ExperienceYearsMin.Value;
        if (job.ExperienceYearsMax.HasValue)
            msg.ExperienceYearsMax = job.ExperienceYearsMax.Value;
        if (job.PostedAt.HasValue)
            msg.PostedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(job.PostedAt.Value, DateTimeKind.Utc));

        foreach (var tag in job.Tags)
        {
            msg.Tags.Add(tag.Name);
        }

        return msg;
    }
}
