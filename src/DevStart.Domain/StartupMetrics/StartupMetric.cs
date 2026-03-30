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
        public static StartupMetric Create(Guid startupId, MetricType metricType, decimal value, DateTime createdAt)
        {
            return new StartupMetric
            {
                Id = Guid.NewGuid(),
                StartupId = startupId,
                MetricType = metricType,
                Value = value,
                CreatedAt = createdAt
            };
        }
        public StartupMetric()
        {
            
        }
    }
}
