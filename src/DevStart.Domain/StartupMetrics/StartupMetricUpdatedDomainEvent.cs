using DevStart.SharedKernel;

namespace DevStart.Domain.StartupMetrics
{
    public sealed record StartupMetricUpdatedDomainEvent(Guid MetricId) : IDomainEvent;
}
