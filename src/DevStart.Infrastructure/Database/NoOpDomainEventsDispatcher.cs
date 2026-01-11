using DevStart.Infrastructure.DomainEvents;
using DevStart.SharedKernel;

namespace DevStart.Infrastructure.Database
{
    internal sealed class NoOpDomainEventsDispatcher : IDomainEventsDispatcher
    {
        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}