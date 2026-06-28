using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;

namespace DevStart.Application.Admin.Valuation
{
    /// <summary>One benchmark row as returned by the admin read endpoints.</summary>
    public sealed class ValuationBenchmarkResponse
    {
        public Guid Id { get; init; }
        public BenchmarkMetricType MetricType { get; init; }
        public Industry Industry { get; init; }
        public StartupStage? Stage { get; init; }
        public decimal Value { get; init; }
        public string? Currency { get; init; }
        public DateTime EffectiveFrom { get; init; }
        public string Source { get; init; } = null!;
        public DateTime CreatedAt { get; init; }
        public Guid? CreatedByUserId { get; init; }
    }
}
