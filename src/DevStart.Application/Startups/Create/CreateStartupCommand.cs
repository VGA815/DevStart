using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;

namespace DevStart.Application.Startups.Create
{
    public sealed class CreateStartupCommand : ICommand<Guid>
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public string PublicEmail { get; set; } = null!;
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public bool IsStopped { get; set; }
        public StartupStage Stage { get; set; }
        public List<string>? SocialMediaLinks { get; set; }
        public StartupLocation? Location { get; set; }
        public string? BillingEmail { get; set; }
        public Guid? AvatarId { get; set; }
        public string ProductName { get; set; } = null!;
        public string ProductProblemSolution { get; set; } = null!;
        public List<string> Stack { get; set; } = [];
        public string ProductValueProposition { get; set; } = null!;
        public string ProductDifferentiators { get; set; } = null!;
        public decimal? Tam { get; set; }
        public decimal? Sam { get; set; }
        public decimal? Som { get; set; }
        public decimal? MarketGrowthRate { get; set; }
        public bool HasPatents { get; set; }
        public Industry Industry { get; set; }
        public decimal? TargetRoundAmount { get; set; }
        public bool HasStrategicPartnerships { get; set; }

        public CreateStartupCommand(Guid userId, string name, string publicEmail, string? description, string? url, bool isStopped, StartupStage stage, List<string>? socialMediaLinks, StartupLocation? location,
            string? billingEmail, Guid? avatarId, string? shortDescription, string productName, string productProblemSolution, List<string> stack, string productValueProposition, string productDifferentiators,
            decimal? tam = null, decimal? sam = null, decimal? som = null,
            decimal? marketGrowthRate = null, bool hasPatents = false,
            Industry industry = Industry.Other, decimal? targetRoundAmount = null, bool hasStrategicPartnerships = false)
        {
            UserId = userId;
            Name = name;
            PublicEmail = publicEmail;
            Description = description;
            ShortDescription = shortDescription;
            Url = url;
            IsStopped = isStopped;
            Stage = stage;
            Location = location;
            BillingEmail = billingEmail;
            AvatarId = avatarId;
            ProductName = productName;
            SocialMediaLinks = socialMediaLinks;
            ProductProblemSolution = productProblemSolution;
            Stack = stack;
            ProductValueProposition = productValueProposition;
            ProductDifferentiators = productDifferentiators;
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
