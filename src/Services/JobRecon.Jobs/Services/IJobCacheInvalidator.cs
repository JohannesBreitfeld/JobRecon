namespace JobRecon.Jobs.Services;

public interface IJobCacheInvalidator
{
    Task InvalidateJobDataAsync(CancellationToken cancellationToken = default);
    Task InvalidateJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}
