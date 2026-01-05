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
    }
}
