using DevStart.SharedKernel;

namespace DevStart.Domain.Startups
{
    public sealed class Startup : Entity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string PublicEmail { get; set; } = null!;
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public bool IsStopped { get; set; }
        public StartupStage Stage { get; set; }
        public List<string>? SocialMediaLinks { get; set; } = [];
        public StartupLocation? Location { get; set; }
        public string? BillingEmail { get; set; }
        public Guid? AvatarId { get; set; }
        public decimal? Tam { get; set; }
        public decimal? Sam { get; set; }
        public decimal? Som { get; set; }
        public decimal? MarketGrowthRate { get; set; }
        public bool HasPatents { get; set; }

        /// <summary>Sector — feeds sector-specific valuation constants. Defaults to <see cref="Industry.Other"/>.</summary>
        public Industry Industry { get; set; }

        /// <summary>Target raising amount for the current round (RUB). Used for the VC Method pre/post-money split.</summary>
        public decimal? TargetRoundAmount { get; set; }

        /// <summary>Whether the startup has strategic partnerships — the fifth Berkus factor.</summary>
        public bool HasStrategicPartnerships { get; set; }

        /// <summary>
        /// ИНН of the legal entity the startup says it operates as (SC-66). Digits only, check-digit
        /// validated on write. It is a declaration, not a proof of control: it lets the platform
        /// compare the declared entity against the rightsholder of an IP record, and nothing more.
        /// </summary>
        public string? Inn { get; set; }

        /// <summary>ОГРН of the same declared entity. Same standing as <see cref="Inn"/>.</summary>
        public string? Ogrn { get; set; }

        public bool IsBanned { get; set; }
        public string? BanReason { get; set; }
        public DateTime? BannedAt { get; set; }
        public DateTime? BanExpiresAt { get; set; }
        public Guid? BannedByUserId { get; set; }

        /// <summary>
        /// End of a paid featured placement (the Promotion one-time service, SC-49). A featured
        /// startup sorts to the front of public discovery until this passes.
        /// </summary>
        public DateTime? FeaturedUntil { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// True while a moderation ban is in force. Distinct from <see cref="IsStopped"/> (owner-controlled).
        /// A temporary ban whose <see cref="BanExpiresAt"/> has passed is treated as lifted ("lazy" expiry).
        /// </summary>
        public bool IsCurrentlyBanned(DateTime utcNow) =>
            IsBanned && (BanExpiresAt is null || BanExpiresAt > utcNow);

        /// <summary>
        /// True while a paid featured placement is in force. Expiry is "lazy" like a temporary ban:
        /// the flag is compared against the clock at query time, so nothing has to sweep it.
        /// </summary>
        public bool IsFeatured(DateTime utcNow) => FeaturedUntil is not null && FeaturedUntil > utcNow;

        /// <summary>
        /// Starts (or tops up) a featured placement. Buying promotion again while one is running adds
        /// the new days on top of the remaining ones, so nothing already paid for is truncated.
        /// </summary>
        public void Feature(int days, DateTime utcNow)
        {
            DateTime from = IsFeatured(utcNow) ? FeaturedUntil!.Value : utcNow;
            FeaturedUntil = from.AddDays(days);
            UpdatedAt = utcNow;
        }

        /// <summary>Ends a featured placement — used when the promotion payment is refunded or cancelled.</summary>
        public void ClearFeature(DateTime utcNow)
        {
            FeaturedUntil = null;
            UpdatedAt = utcNow;
        }

        public Result Ban(string reason, DateTime? expiresAt, Guid byUserId, DateTime utcNow)
        {
            if (IsCurrentlyBanned(utcNow))
            {
                return Result.Failure(StartupErrors.AlreadyBanned);
            }
            if (expiresAt is not null && expiresAt <= utcNow)
            {
                return Result.Failure(StartupErrors.BanExpiryInPast);
            }

            IsBanned = true;
            BanReason = reason;
            BannedAt = utcNow;
            BanExpiresAt = expiresAt;
            BannedByUserId = byUserId;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        public Result Unban(DateTime utcNow)
        {
            if (!IsBanned)
            {
                return Result.Failure(StartupErrors.NotBanned);
            }

            IsBanned = false;
            BanReason = null;
            BannedAt = null;
            BanExpiresAt = null;
            BannedByUserId = null;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        public static Startup Create(
            string name, string publicEmail, string? description,
            string? url, StartupStage startupStage, StartupLocation? location,
            string? billingEmail, Guid? avatarId, DateTime createdAt,
            List<string>? socialMediaLinks, string? shortDescription,
            decimal? tam = null, decimal? sam = null, decimal? som = null,
            decimal? marketGrowthRate = null, bool hasPatents = false,
            Industry industry = Industry.Other, decimal? targetRoundAmount = null,
            bool hasStrategicPartnerships = false, string? inn = null, string? ogrn = null)
            => new ()
            {
                Id = Guid.NewGuid(),
                Name = name,
                ShortDescription = shortDescription,
                AvatarId = avatarId,
                BillingEmail = billingEmail,
                CreatedAt = createdAt,
                Description = description,
                IsStopped = false,
                Location = location,
                PublicEmail = publicEmail,
                SocialMediaLinks = socialMediaLinks,
                Stage = startupStage,
                UpdatedAt = createdAt,
                Url = url,
                Tam = tam,
                Sam = sam,
                Som = som,
                MarketGrowthRate = marketGrowthRate,
                HasPatents = hasPatents,
                Industry = industry,
                TargetRoundAmount = targetRoundAmount,
                HasStrategicPartnerships = hasStrategicPartnerships,
                Inn = inn,
                Ogrn = ogrn
            };
        public Startup() { }
    }
}
