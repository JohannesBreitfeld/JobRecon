using JobRecon.Matching.Contracts;
using JobRecon.Protos.Jobs;

namespace JobRecon.Matching.Services;

public sealed class JobsClient(
    JobsGrpc.JobsGrpcClient grpcClient,
    ILogger<JobsClient> logger) : IJobsClient
{
    public async Task<JobListDto?> GetActiveJobsAsync(int limit, int offset, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await grpcClient.GetActiveJobsAsync(
                new GetActiveJobsRequest { Limit = limit, Offset = offset },
                cancellationToken: cancellationToken);

            return new JobListDto(
                response.Jobs.Select(MapToJobDto).ToList(),
                response.TotalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active jobs via gRPC");
            return null;
        }
    }

    public async Task<JobDto?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await grpcClient.GetJobAsync(
                new GetJobRequest { JobId = jobId.ToString() },
                cancellationToken: cancellationToken);

            if (response.Job is null)
            {
                logger.LogWarning("Job {JobId} not found", jobId);
                return null;
            }

            return MapToJobDto(response.Job);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting job {JobId} via gRPC", jobId);
            return null;
        }
    }

    public async Task<List<JobDto>> GetJobsByIdsAsync(IEnumerable<Guid> jobIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetJobsByIdsRequest();
            foreach (var id in jobIds)
                request.JobIds.Add(id.ToString());

            var response = await grpcClient.GetJobsByIdsAsync(request, cancellationToken: cancellationToken);
            return response.Jobs.Select(MapToJobDto).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting {Count} jobs by IDs via gRPC", jobIds.Count());
            return [];
        }
    }

    private static JobDto MapToJobDto(JobMessage msg)
    {
        return new JobDto(
            Guid.Parse(msg.Id),
            msg.Title,
            msg.HasDescription ? msg.Description : null,
            msg.HasLocation ? msg.Location : null,
            msg.HasWorkLocationType ? msg.WorkLocationType : null,
            msg.HasEmploymentType ? msg.EmploymentType : null,
            msg.HasSalaryMin ? (decimal)msg.SalaryMin : null,
            msg.HasSalaryMax ? (decimal)msg.SalaryMax : null,
            msg.HasSalaryCurrency ? msg.SalaryCurrency : null,
            msg.HasRequiredSkills ? msg.RequiredSkills : null,
            msg.HasExperienceYearsMin ? msg.ExperienceYearsMin : null,
            msg.HasExperienceYearsMax ? msg.ExperienceYearsMax : null,
            msg.PostedAt?.ToDateTime(),
            msg.HasExternalUrl ? msg.ExternalUrl : null,
            new CompanyDto(
                Guid.Parse(msg.Company.Id),
                msg.Company.Name,
                msg.Company.HasLogoUrl ? msg.Company.LogoUrl : null,
                msg.Company.HasIndustry ? msg.Company.Industry : null),
            msg.Tags.ToList());
    }
}
