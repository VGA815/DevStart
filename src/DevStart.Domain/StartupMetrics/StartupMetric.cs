using DevStart.SharedKernel;

namespace DevStart.Domain.StartupMetrics
{
    public sealed class StartupMetric : Entity
    {
        public Guid Id { get; set; }
        public Guid StartupId { get; set; }
        public MetricType MetricType { get; set; }
        public decimal Value { get; set; }
        public DateTime CreatedAt { get; set; }

        public StartupMetric(Guid startupId, MetricType metricType, decimal value, DateTime createdAt)
        {
            Id = Guid.NewGuid();
            MetricType = metricType;
            Value = value;
            CreatedAt = createdAt;
            StartupId = startupId;
        }
    }
}
