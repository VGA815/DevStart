using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;

namespace DevStart.Application.Startups.Update
{
    public sealed class UpdateStartupCommand : ICommand
    {
        public Guid StartupId { get; set; }
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

        public UpdateStartupCommand(Guid startupId, string name, string publicEmail, string description, string url, bool isStopped, StartupStage startupStage,
            List<string> socialMediaLinks, StartupLocation location, string billingEmail, string avatarUrl)
        {
            StartupId = startupId;
            Name = name;
            PublicEmail = publicEmail;
            Description = description;
            Url = url;
            IsStopped = isStopped;
            Stage = startupStage;
            SocialMediaLinks = socialMediaLinks;
            Location = location;
            BillingEmail = billingEmail;
            AvatarUrl = avatarUrl;
        }
    }
}
