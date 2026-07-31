using DevStart.Domain.StartupCommunityStandards;
using DevStart.Domain.Startups;

namespace DevStart.Application.Startups.GetAll
{
    public sealed class StartupResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string PublicEmail { get; init; } = null!;
        public string? Description { get; init; } = null!;
        public string? Url { get; init; } = null!;
        public string? ShortDescription { get; set; }
        public bool IsStopped { get; init; }
        public StartupStage Stage { get; init; }
        public List<string>? SocialMediaLinks { get; init; } = [];
        public StartupLocation? Location { get; init; }
        public string? BillingEmail { get; init; } = null!;
        public Guid? AvatarId { get; init; }
        public decimal? Tam { get; init; }
        public decimal? Sam { get; init; }
        public decimal? Som { get; init; }
        public decimal? MarketGrowthRate { get; init; }
        public bool HasPatents { get; init; }

        /// <summary>
        /// Share of the community-standards checklist completed, 0–100. Served from the projection
        /// refreshed by writes and the nightly sweep, so it can trail the live checklist slightly;
        /// <c>api/startups/{id}/community</c> is the authoritative read.
        /// </summary>
        public decimal CommunityStandardsPercent { get; init; }

        public CommunityStandardsLevel CommunityStandardsLevel { get; init; }

        /// <summary>
        /// Whether a paid featured placement (the Promotion one-time service) is currently running.
        /// Featured startups are listed first.
        /// </summary>
        public bool IsFeatured { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}