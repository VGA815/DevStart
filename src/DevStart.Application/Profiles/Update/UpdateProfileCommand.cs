using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Profiles.Update
{
    public sealed class UpdateProfileCommand : ICommand
    {
        public Guid UserId { get; set; }
        public string? Url { get; set; }
        public string Name { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public bool IsPublic { get; set; }
        public bool IsAvailableForHire { get; set; }
        public List<string> SocialMediaLinks { get; set; } = [];

        public UpdateProfileCommand(Guid userId, string? url, string name, string? avatarUrl, string? bio, bool isPublic, bool isAvailableForHire, List<string> socialMediaLinks)
        {
            UserId = userId;
            Url = url;
            Name = name;
            AvatarUrl = avatarUrl;
            Bio = bio;
            IsPublic = isPublic;
            IsAvailableForHire = isAvailableForHire;
            SocialMediaLinks = socialMediaLinks;
        }
    }
}
