using DevStart.Domain.Startups;

namespace DevStart.Application.Startups.GetById
{
    public sealed class StartupResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = null!;
        public string PublicEmail { get; init; } = null!;
        public string? Description { get; init; }
        public string? Url { get; init; }
        public string? ShortDescription { get; set; }
        public bool IsStopped { get; init; }
        public StartupStage Stage { get; init; }
        public List<string>? SocialMediaLinks { get; init; } = [];
        public StartupLocation? Location { get; init; }
        public string? BillingEmail { get; init; }
        public Guid? AvatarId { get; init; }
        public decimal? Tam { get; init; }
        public decimal? Sam { get; init; }
        public decimal? Som { get; init; }
        public decimal? MarketGrowthRate { get; init; }
        public bool HasPatents { get; init; }

        /// <summary>
        /// Whether a paid featured placement is currently running. This response is cached for
        /// <see cref="Abstractions.Caching.CacheTtl.Default"/>; the cache is dropped when a promotion is
        /// bought or refunded, so only natural expiry can leave the badge up for up to that long.
        /// </summary>
        public bool IsFeatured { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}