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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public static Startup Create(
            string name, string publicEmail, string? description,
            string? url, StartupStage startupStage, StartupLocation? location,
            string? billingEmail, Guid? avatarId, DateTime createdAt,
            List<string>? socialMediaLinks, string? shortDescription,
            decimal? tam = null, decimal? sam = null, decimal? som = null)
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
                Som = som
            };
        public Startup() { }
    }
}
