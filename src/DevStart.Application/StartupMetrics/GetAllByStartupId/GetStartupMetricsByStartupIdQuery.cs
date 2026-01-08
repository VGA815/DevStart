using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupMetrics.GetAllByStartupId
{
    public sealed record GetStartupMetricsByStartupIdQuery(Guid StartupId) : IQuery<List<StartupMetricResponse>>;
}
