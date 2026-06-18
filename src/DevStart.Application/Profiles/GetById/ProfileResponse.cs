namespace DevStart.Application.Profiles.GetById
{
    public sealed class ProfileResponse
    {
        public Guid UserId { get; set; }
        public string? Url { get; set; }
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public List<string> SocialMediaLinks { get; set; } = [];
        public string? LinkedInUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? GitHubUrl { get; set; }
        public string? TelegramUrl { get; set; }
        public bool IsPublic { get; set; }
        public bool IsAvailableForHire { get; set; }
        public Guid? AvatarId { get; set; }
        public int ViewCount { get; set; }
    }
}