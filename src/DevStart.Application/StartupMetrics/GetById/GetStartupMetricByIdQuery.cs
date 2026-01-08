using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupMetrics.GetById
{
    public sealed record GetStartupMetricByIdQuery(Guid MetricId) : IQuery<StartupMetricResponse>;
}
