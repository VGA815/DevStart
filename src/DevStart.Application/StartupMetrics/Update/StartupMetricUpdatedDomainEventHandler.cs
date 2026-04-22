using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.StartupMetrics;
using DevStart.SharedKernel;

namespace DevStart.Application.StartupMetrics.Update
{
    internal sealed class StartupMetricUpdatedDomainEventHandler(ICacheService cache)
        : IDomainEventHandler<StartupMetricUpdatedDomainEvent>
    {
        public Task Handle(StartupMetricUpdatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
            cache.RemoveAsync(CacheKeys.StartupMetric(domainEvent.MetricId), cancellationToken);
    }
}
