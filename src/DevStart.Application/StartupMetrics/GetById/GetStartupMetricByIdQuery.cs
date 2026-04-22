using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupMetrics.GetById
{
    public sealed record GetStartupMetricByIdQuery(Guid MetricId) : IQuery<StartupMetricResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.StartupMetric(MetricId);
        public TimeSpan Expiration => CacheTtl.Default;
    }
}
