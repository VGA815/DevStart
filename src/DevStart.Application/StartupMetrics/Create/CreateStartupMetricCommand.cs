using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMetrics;

namespace DevStart.Application.StartupMetrics.Create
{
    public sealed class CreateStartupMetricCommand : ICommand<Guid>
    {
        public Guid StartupId { get; set; }
        public MetricType MetricType { get; set; }
        public decimal Value { get; set; }
    }
}
