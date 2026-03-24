namespace JobRecon.Matching.Contracts;

public interface IMatchingService
{
    Task<RecommendationsResponse> GetRecommendationsAsync(
        Guid userId,
        GetRecommendationsRequest request,
        CancellationToken cancellationToken = default);

    Task<JobRecommendation?> GetJobMatchScoreAsync(
        Guid userId,
        Guid jobId,
        CancellationToken cancellationToken = default);
}

public interface IProfileClient
{
    Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IJobsClient
{
    Task<JobListDto?> GetActiveJobsAsync(int limit, int offset, CancellationToken cancellationToken = default);
    Task<JobDto?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<List<JobDto>> GetJobsByIdsAsync(IEnumerable<Guid> jobIds, CancellationToken cancellationToken = default);
}
