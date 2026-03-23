using JobRecon.Contracts.Events;

namespace JobRecon.Matching.Services;

public interface IEventPublisher
{
    Task PublishJobMatchedAsync(JobMatchedEvent eventData, CancellationToken ct = default);
}
