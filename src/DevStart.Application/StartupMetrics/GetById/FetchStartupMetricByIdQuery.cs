using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupMetrics.GetById
{
    /// <summary>
    /// Internal, viewer-independent metric read. This is the cached unit of work and carries NO
    /// authorization gate. Must not be exposed via an endpoint — public access goes through
    /// <see cref="GetStartupMetricByIdQuery"/>, which runs the premium Pro/member gate before
    /// returning the result, so a warm cache can never bypass the paywall.
    /// </summary>
    internal sealed record FetchStartupMetricByIdQuery(Guid MetricId) : IQuery<StartupMetricResponse>, ICacheableQuery
    {
        public string CacheKey => CacheKeys.StartupMetric(MetricId);
        public TimeSpan Expiration => CacheTtl.Default;
    }
}
