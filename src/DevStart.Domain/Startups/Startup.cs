using DevStart.SharedKernel;

namespace DevStart.Domain.Startups
{
    public sealed class Startup : Entity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string PublicEmail { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public bool IsStopped { get; set; }
        public StartupStage Stage { get; set; }
        public List<string> SocialMediaLinks { get; set; } = [];
        public StartupLocation Location { get; set; }
        public string BillingEmail { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
