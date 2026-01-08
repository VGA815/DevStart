using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupMetrics.Delete
{
    public sealed record DeleteStartupMetricCommand(Guid MetricId) : ICommand;
}
