using DevStart.SharedKernel;

namespace DevStart.Domain.Profiles
{
    public sealed class Profile : Entity
    {
        public Guid UserId { get; set; }
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? Url { get; set; }
        public List<string> SocialMediaLinks { get; set; } = [];
        public bool IsAvailableForHire { get; set; }
        public bool IsPublic { get; set; }
        public Guid? AvatarId { get; set; }

        public static Profile Create(
            Guid userId,
            string? name,
            string? bio,
            string? url,
            bool isAvailableForHire,
            bool isPublic,
            Guid? avatarId)
            => new()
            {
                UserId = userId,
                Name = name,
                Bio = bio,
                Url = url,
                IsAvailableForHire = isAvailableForHire,
                IsPublic = isPublic,
                AvatarId = avatarId
            };
        public Profile()
        {
            
        }
    }
}
