using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupRoadmapItems.Delete
{
    public sealed record DeleteStartupRoadmapItemCommand(Guid ItemId) : ICommand;
}
