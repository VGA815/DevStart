using DevStart.Domain.Startups;

namespace DevStart.Application.Startups.GetById
{
    public sealed class StartupResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string PublicEmail { get; init; } = null!;
        public string Description { get; init; } = null!;
        public string Url { get; init; } = null!;
        public bool IsStopped { get; init; }
        public StartupStage Stage { get; init; }
        public List<string> SocialMediaLinks { get; init; } = [];
        public StartupLocation Location { get; init; }
        public string BillingEmail { get; init; } = null!;
        public string AvatarUrl { get; init; } = null!;
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}