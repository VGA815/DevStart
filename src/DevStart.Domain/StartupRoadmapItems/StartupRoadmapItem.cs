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
        public string? Desription { get; set; }
        public RoadmapItemStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime TargetDate { get; set; }
        public StartupRoadmapItem()
        {
            
        }

        public StartupRoadmapItem(Guid startupId, StartupStage startupStage, string title, string? description, RoadmapItemStatus status, DateTime createdAt, DateTime targetDate)
        {
            Id = Guid.NewGuid();
            StartupId = startupId;
            StartupStage = startupStage;
            Title = title;
            Desription = description;
            Status = status;
            CreatedAt = createdAt;
            TargetDate = targetDate;
        }
    }
}
