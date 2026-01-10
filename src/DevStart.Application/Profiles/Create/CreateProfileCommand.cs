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
    }
}
