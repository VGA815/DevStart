using DevStart.Domain.Startups;
using DevStart.SharedKernel;

namespace DevStart.Domain.StartupRoadmapItems
{
    public sealed class StartupRoadmapItem : Entity
    {
        public Guid Id { get; set; }
        public Guid StartupId { get; set; }
        public StartupStage StartupStage { get; set; }
        public string Title { get; set; }
        public string Desription { get; set; }
        public RoadmapItemStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime TargetDate { get; set; }
    }
}
