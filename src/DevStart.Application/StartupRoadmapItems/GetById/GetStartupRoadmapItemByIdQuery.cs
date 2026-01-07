using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupRoadmapItems.GetById
{
    public sealed record GetStartupRoadmapItemByIdQuery(Guid ItemId) : IQuery<StartupRoadmapItemResponse>;
}
