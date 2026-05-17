using DevStart.SharedKernel;

namespace DevStart.Domain.Experts
{
    public sealed class ExpertProfile : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public bool IsPublic { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? GitHubUrl { get; set; }
        public string? TelegramUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ExpertProfile()
        {
        }

        public static ExpertProfile Create(
            Guid userId,
            string displayName,
            string? bio,
            string? website,
            bool isPublic,
            string? linkedInUrl,
            string? twitterUrl,
            string? gitHubUrl,
            string? telegramUrl,
            DateTime createdAt)
            => new()
            {
                Id = userId,
                UserId = userId,
                DisplayName = displayName,
                Bio = bio,
                Website = website,
                IsPublic = isPublic,
                LinkedInUrl = linkedInUrl,
                TwitterUrl = twitterUrl,
                GitHubUrl = gitHubUrl,
                TelegramUrl = telegramUrl,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

        public void Update(
            string displayName,
            string? bio,
            string? website,
            bool isPublic,
            string? linkedInUrl,
            string? twitterUrl,
            string? gitHubUrl,
            string? telegramUrl,
            DateTime updatedAt)
        {
            DisplayName = displayName;
            Bio = bio;
            Website = website;
            IsPublic = isPublic;
            LinkedInUrl = linkedInUrl;
            TwitterUrl = twitterUrl;
            GitHubUrl = gitHubUrl;
            TelegramUrl = telegramUrl;
            UpdatedAt = updatedAt;
        }
    }
}
