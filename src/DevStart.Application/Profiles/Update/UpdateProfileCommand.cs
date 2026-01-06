using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Profiles.Update
{
    public sealed class UpdateProfileCommand : ICommand
    {
        public Guid UserId { get; set; }
        public string Url { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string AvatarUrl { get; set; } = null!;
        public string Bio { get; set; } = null!;
        public bool IsPublic { get; set; }
        public bool IsAvailableForHire { get; set; }
        public List<string> SocialMediaLinks { get; set; } = [];
    }
}
