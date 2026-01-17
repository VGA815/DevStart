using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;

namespace DevStart.Application.Startups.Create
{
    public sealed class CreateStartupCommand : ICommand<Guid>
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public string PublicEmail { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Url { get; set; } = null!;
        public bool IsStopped { get; set; }
        public StartupStage Stage { get; set; }
        public List<string> SocialMediaLinks { get; set; } = null!;
        public StartupLocation Location { get; set; }
        public string BillingEmail { get; set; } = null!;
        public string AvatarUrl { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string ProductProblemSolution { get; set; } = null!;
        public List<string> Stack { get; set; } = [];
        public string ProductValueProposition { get; set; } = null!;
        public string ProductDifferentiators { get; set; } = null!;

        public CreateStartupCommand(Guid userId, string name, string publicEmail, string description, string url, bool isStopped, StartupStage stage, List<string> socialMediaLinks, StartupLocation location,
            string billingEmail, string avatarUrl, string productName, string productProblemSolution, List<string> stack, string productValueProposition, string productDifferentiators)
        {
            UserId = userId;
            Name = name;
            PublicEmail = publicEmail;
            Description = description;
            Url = url;
            IsStopped = isStopped;
            Stage = stage;
            Location = location;
            BillingEmail = billingEmail;
            AvatarUrl = avatarUrl;
            ProductName = productName;
            SocialMediaLinks = socialMediaLinks;
            ProductProblemSolution = productProblemSolution;
            Stack = stack;
            ProductValueProposition = productValueProposition;
            ProductDifferentiators = productDifferentiators;
        }
    }
}
