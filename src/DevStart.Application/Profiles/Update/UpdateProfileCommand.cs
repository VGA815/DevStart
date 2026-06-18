using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Profiles.Update
{
    public sealed class UpdateProfileCommand : ICommand
    {
        public Guid UserId { get; set; }
        public string? Url { get; set; }
        public string Name { get; set; } = null!;
        public Guid? AvatarId { get; set; }
        public string? Bio { get; set; }
        public bool IsPublic { get; set; }
        public bool IsAvailableForHire { get; set; }
        public List<string> SocialMediaLinks { get; set; } = [];
        public string? LinkedInUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? GitHubUrl { get; set; }
        public string? TelegramUrl { get; set; }

        public UpdateProfileCommand(Guid userId, string? url, string name, Guid? avatarId, string? bio, bool isPublic, bool isAvailableForHire, List<string> socialMediaLinks,
            string? linkedInUrl = null, string? twitterUrl = null, string? gitHubUrl = null, string? telegramUrl = null)
        {
            UserId = userId;
            Url = url;
            Name = name;
            AvatarId = avatarId;
            Bio = bio;
            IsPublic = isPublic;
            IsAvailableForHire = isAvailableForHire;
            SocialMediaLinks = socialMediaLinks;
            LinkedInUrl = linkedInUrl;
            TwitterUrl = twitterUrl;
            GitHubUrl = gitHubUrl;
            TelegramUrl = telegramUrl;
        }
    }
}
