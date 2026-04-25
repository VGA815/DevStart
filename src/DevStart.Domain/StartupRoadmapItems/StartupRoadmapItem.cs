using DevStart.Domain.Startups;
using DevStart.SharedKernel;

namespace DevStart.Domain.StartupRoadmapItems
{
    public sealed class StartupRoadmapItem : Entity
    {
        public Guid Id { get; set; }
        public Guid StartupId { get; set; }
        public StartupStage StartupStage { get; set; }
        public string Title { get; set; } = null!;
        public string? Desription { get; set; }
        public RoadmapItemStatus Status { get; set; }
        public decimal? TargetAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime TargetDate { get; set; }
        public StartupRoadmapItem()
        {

        }

        public static StartupRoadmapItem Create(
            Guid startupId, StartupStage startupStage, string title, string? desription,
            RoadmapItemStatus status, decimal? targetAmount, DateTime createdAt, DateTime targetDate)
            => new ()
            {
                Id = Guid.NewGuid(),
                CreatedAt = createdAt,
                Desription = desription,
                StartupId = startupId,
                StartupStage = startupStage,
                Status = status,
                TargetAmount = targetAmount,
                TargetDate = targetDate,
                Title = title
            };
    }
}
