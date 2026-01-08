using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMetrics;

namespace DevStart.Application.StartupMetrics.Update
{
    public sealed class UpdateStartupMetricCommand : ICommand
    {
        public Guid Id { get; set; }
        public Guid StartupId { get; set; }
        public MetricType MetricType { get; set; }
        public decimal Value { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
