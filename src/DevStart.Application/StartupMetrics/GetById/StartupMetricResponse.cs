using DevStart.Domain.StartupMetrics;

namespace DevStart.Application.StartupMetrics.GetById
{
    public sealed class StartupMetricResponse
    {
        public Guid Id { get; init; }
        public Guid StartupId { get; init; }
        public MetricType MetricType { get; init; }
        public decimal Value { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}