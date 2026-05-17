using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Experts;

namespace DevStart.Application.ExpertProfiles.Update
{
    public sealed class UpdateExpertProfileCommand : ICommand
    {
        public string DisplayName { get; set; } = null!;
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public bool IsPublic { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? GitHubUrl { get; set; }
        public string? TelegramUrl { get; set; }
        public List<ExpertSpecialization> Specializations { get; set; } = new();

        public UpdateExpertProfileCommand(
            string displayName,
            string? bio,
            string? website,
            bool isPublic,
            string? linkedInUrl,
            string? twitterUrl,
            string? gitHubUrl,
            string? telegramUrl,
            List<ExpertSpecialization> specializations)
        {
            DisplayName = displayName;
            Bio = bio;
            Website = website;
            IsPublic = isPublic;
            LinkedInUrl = linkedInUrl;
            TwitterUrl = twitterUrl;
            GitHubUrl = gitHubUrl;
            TelegramUrl = telegramUrl;
            Specializations = specializations;
        }
    }
}
