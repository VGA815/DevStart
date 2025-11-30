using DevStart.SharedKernel;

namespace DevStart.Domain.Profiles
{
    public sealed class Profile : Entity
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
        public string Url { get; set; }
        public List<string> SocialMediaLinks { get; set; }
        public bool IsAvailableForHire { get; set; }
        public bool IsPublic { get; set; }
        public string AvatarUrl { get; set; }
    }
}
