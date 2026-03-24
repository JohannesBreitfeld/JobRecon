using JobRecon.Domain.Common;
using MediatR;

namespace JobRecon.Jobs.Infrastructure;

public sealed class MediatRDomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) =>
        publisher.Publish(domainEvent, cancellationToken);
}
