using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupRoadmapItems;
using DevStart.Domain.Startups;

namespace DevStart.Application.StartupRoadmapItems.Update
{
    public sealed class UpdateStartupRoadmapItemCommand : ICommand
    {
        public Guid ItemId { get; set; }
        public Guid StartupId { get; set; }
        public StartupStage StartupStage { get; set; }
        public string Title { get; set; } = null!;
        public string? Desription { get; set; }
        public RoadmapItemStatus Status { get; set; }
        public DateTime TargetDate { get; set; }

        public UpdateStartupRoadmapItemCommand(Guid itemId, Guid startupId, StartupStage startupStage, string title, string? description, RoadmapItemStatus status, DateTime targetDate)
        {
            ItemId = itemId;
            StartupId = startupId;
            StartupStage = startupStage;
            Title = title;
            Desription = description;
            Status = status;
            TargetDate = targetDate;
        }
    }
}
