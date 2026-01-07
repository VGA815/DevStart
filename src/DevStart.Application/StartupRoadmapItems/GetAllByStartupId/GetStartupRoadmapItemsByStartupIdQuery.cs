using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupRoadmapItems.GetAllByStartupId
{
    public sealed record GetStartupRoadmapItemsByStartupIdQuery(Guid StartupId) : IQuery<List<StartupRoadmapItemResponse>>;
}
