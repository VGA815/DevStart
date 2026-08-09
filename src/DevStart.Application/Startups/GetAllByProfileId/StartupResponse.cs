using DevStart.Domain.Startups;

namespace DevStart.Application.Startups.GetAllByProfileId
{
    public sealed class StartupResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string PublicEmail { get; init; } = null!;
        public string? Description { get; init; }
        public string? ShortDescription { get; set; }
        public string? Url { get; init; } = null!;
        public bool IsStopped { get; init; }
        public StartupStage Stage { get; init; }
        public List<string>? SocialMediaLinks { get; init; } = [];
        public StartupLocation? Location { get; init; }
        public Guid? AvatarId { get; init; }
        public decimal? Tam { get; init; }
        public decimal? Sam { get; init; }
        public decimal? Som { get; init; }
        public decimal? MarketGrowthRate { get; init; }
        public bool HasPatents { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}