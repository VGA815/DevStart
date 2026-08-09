using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Experts;

namespace DevStart.Application.ExpertProfiles.Update
{
    public sealed class UpdateExpertProfileCommand : ICommand
    {
        public List<ExpertSpecialization> Specializations { get; set; } = new();

        // Stored on the shared Profile rather than on the expert profile — see ProfilePersonalDetails.
        public string DisplayName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public bool IsPublic { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? GitHubUrl { get; set; }
        public string? TelegramUrl { get; set; }

        public UpdateExpertProfileCommand(
            List<ExpertSpecialization> specializations,
            string displayName,
            string? bio = null,
            string? website = null,
            bool isPublic = true,
            string? linkedInUrl = null,
            string? twitterUrl = null,
            string? gitHubUrl = null,
            string? telegramUrl = null)
        {
            Specializations = specializations;
            DisplayName = displayName;
            Bio = bio;
            Website = website;
            IsPublic = isPublic;
            LinkedInUrl = linkedInUrl;
            TwitterUrl = twitterUrl;
            GitHubUrl = gitHubUrl;
            TelegramUrl = telegramUrl;
        }
    }
}
