using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupMetrics.GetById
{
    // Public, authorization-gated entry point for a single startup metric. NOT cacheable: the
    // premium-MetricType Pro/member gate in the handler must run on every request. The actual
    // metric read is cached one layer down via FetchStartupMetricByIdQuery (viewer-independent),
    // so the gate can never be skipped on a cache hit.
    public sealed record GetStartupMetricByIdQuery(Guid MetricId) : IQuery<StartupMetricResponse>;
}
