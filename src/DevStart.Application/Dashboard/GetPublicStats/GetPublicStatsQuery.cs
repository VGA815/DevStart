using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Dashboard.GetPublicStats
{
    public sealed record GetPublicStatsQuery : IQuery<PublicStatsResponse>;
}
