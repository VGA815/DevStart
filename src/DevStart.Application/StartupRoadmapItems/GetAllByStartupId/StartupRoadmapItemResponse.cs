using DevStart.Domain.StartupRoadmapItems;
using DevStart.Domain.Startups;

namespace DevStart.Application.StartupRoadmapItems.GetAllByStartupId
{
    public sealed class StartupRoadmapItemResponse
    {
        public Guid Id { get; init; }
        public Guid StartupId { get; init; }
        public StartupStage StartupStage { get; init; }
        public string Title { get; init; } = null!;
        public string? Desription { get; init; }
        public RoadmapItemStatus Status { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime TargetDate { get; init; }
    }
}