using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;

namespace DevStart.Application.Admin.Valuation.AddValuationBenchmark
{
    /// <summary>
    /// Adds a new (append-only) benchmark version. A correction is a fresh row with a later
    /// <see cref="EffectiveFrom"/>; existing rows are never edited or deleted. For medians,
    /// <see cref="Stage"/> and <see cref="Currency"/> are required; for revenue multiples both must be
    /// absent.
    /// </summary>
    public sealed record AddValuationBenchmarkCommand(
        BenchmarkMetricType MetricType,
        Industry Industry,
        StartupStage? Stage,
        decimal Value,
        string? Currency,
        DateTime EffectiveFrom,
        string Source) : ICommand<Guid>;
}
