using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;

namespace DevStart.Application.Startups.Update
{
    public sealed class UpdateStartupCommand : ICommand
    {
        public Guid StartupId { get; set; }
        public string Name { get; set; } = null!;
        public string PublicEmail { get; set; } = null!;
        public string? ShortDescription { get; set; }
        public string Description { get; set; } = null!;
        public string Url { get; set; } = null!;
        public bool IsStopped { get; set; }
        public StartupStage Stage { get; set; }
        public List<string> SocialMediaLinks { get; set; } = null!;
        public StartupLocation Location { get; set; }
        public string BillingEmail { get; set; } = null!;
        public Guid? AvatarId { get; set; }
        public decimal? Tam { get; set; }
        public decimal? Sam { get; set; }
        public decimal? Som { get; set; }
        public decimal? MarketGrowthRate { get; set; }
        public bool HasPatents { get; set; }

        /// <summary>
        /// Sector. Null means "not supplied" and leaves the stored value alone — unlike the money
        /// fields, an omitted sector cannot be told apart from a deliberate clear, and silently
        /// resetting it to <see cref="Industry.Other"/> would wipe the sector medians, revenue
        /// multiples and competition intensity the scoring engine keys off.
        /// </summary>
        public Industry? Industry { get; set; }
        public decimal? TargetRoundAmount { get; set; }

        /// <summary>Fifth Berkus factor. Null means "not supplied" — see <see cref="Industry"/>.</summary>
        public bool? HasStrategicPartnerships { get; set; }

        public UpdateStartupCommand(Guid startupId, string name, string publicEmail, string description, string url, bool isStopped, StartupStage startupStage,
            List<string> socialMediaLinks, StartupLocation location, string billingEmail, Guid? avatarId, string? shortDescription,
            decimal? tam = null, decimal? sam = null, decimal? som = null,
            decimal? marketGrowthRate = null, bool hasPatents = false,
            Industry? industry = null, decimal? targetRoundAmount = null, bool? hasStrategicPartnerships = null)
        {
            StartupId = startupId;
            Name = name;
            PublicEmail = publicEmail;
            Description = description;
            ShortDescription = shortDescription;
            Url = url;
            IsStopped = isStopped;
            Stage = startupStage;
            SocialMediaLinks = socialMediaLinks;
            Location = location;
            BillingEmail = billingEmail;
            AvatarId = avatarId;
            Tam = tam;
            Sam = sam;
            Som = som;
            MarketGrowthRate = marketGrowthRate;
            HasPatents = hasPatents;
            Industry = industry;
            TargetRoundAmount = targetRoundAmount;
            HasStrategicPartnerships = hasStrategicPartnerships;
        }
    }
}
