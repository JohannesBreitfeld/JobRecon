using JobRecon.Domain.Common;

namespace JobRecon.Jobs.Contracts;

public interface IJobService
{
    Task<Result<JobSearchResponse>> SearchJobsAsync(
        Guid? userId,
        JobSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<JobResponse>> GetJobAsync(
        Guid jobId,
        Guid? userId,
        CancellationToken cancellationToken = default);

    Task<Result<JobResponse>> CreateJobAsync(
        CreateJobRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<List<SavedJobResponse>>> GetSavedJobsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<SavedJobResponse>> SaveJobAsync(
        Guid userId,
        Guid jobId,
        SaveJobRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<SavedJobResponse>> UpdateSavedJobAsync(
        Guid userId,
        Guid jobId,
        UpdateSavedJobRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveSavedJobAsync(
        Guid userId,
        Guid jobId,
        CancellationToken cancellationToken = default);

    Task<Result<JobStatisticsResponse>> GetStatisticsAsync(
        Guid? userId,
        CancellationToken cancellationToken = default);

    Task<Result<List<CompanyResponse>>> GetCompaniesAsync(
        string? search,
        int limit,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyResponse>> GetCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}

public interface IJobSourceService
{
    Task<Result<List<JobSourceResponse>>> GetJobSourcesAsync(
        CancellationToken cancellationToken = default);

    Task<Result<JobSourceResponse>> GetJobSourceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<JobSourceResponse>> CreateJobSourceAsync(
        CreateJobSourceRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<JobSourceResponse>> UpdateJobSourceAsync(
        Guid id,
        UpdateJobSourceRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteJobSourceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result> TriggerFetchAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
