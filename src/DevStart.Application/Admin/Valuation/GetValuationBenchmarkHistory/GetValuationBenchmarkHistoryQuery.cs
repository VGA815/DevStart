using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;

namespace DevStart.Application.Admin.Valuation.GetValuationBenchmarkHistory
{
    /// <summary>
    /// All versions for one (metric, sector, stage) key, newest first — the audit trail behind the
    /// append-only corrections. <see cref="Stage"/> is left null for revenue multiples.
    /// </summary>
    public sealed record GetValuationBenchmarkHistoryQuery(
        BenchmarkMetricType MetricType,
        Industry Industry,
        StartupStage? Stage) : IQuery<List<ValuationBenchmarkResponse>>;
}
