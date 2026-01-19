using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Profiles.Create
{
    public sealed class CreateProfileCommand : ICommand<Guid>
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsAvailableForHire { get; set; }
        public bool IsPublic { get; set; }
        public List<string> SocialMediaLinks { get; set; } = [];
        public string? Url { get; set; }

        public CreateProfileCommand(Guid userId, string name, string? bio, string? avatarUrl, bool isAvailableForHire, bool isPublic, List<string> socialMediaLinks, string? url)
        {
            UserId = userId;
            Name = name;
            Bio = bio;
            AvatarUrl = avatarUrl;
            IsAvailableForHire = isAvailableForHire;
            IsPublic = isPublic;
            SocialMediaLinks = socialMediaLinks;
            Url = url;
        }
    }
}
