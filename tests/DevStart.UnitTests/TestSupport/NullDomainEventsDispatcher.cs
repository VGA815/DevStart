using DevStart.Infrastructure.DomainEvents;
using DevStart.SharedKernel;

namespace DevStart.UnitTests.TestSupport
{
    internal sealed class NullDomainEventsDispatcher : IDomainEventsDispatcher
    {
        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
