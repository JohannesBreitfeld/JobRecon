namespace JobRecon.Jobs.Contracts;

public interface IJobExpirationService
{
    Task<int> ExpireJobsAsync(CancellationToken cancellationToken = default);
}
