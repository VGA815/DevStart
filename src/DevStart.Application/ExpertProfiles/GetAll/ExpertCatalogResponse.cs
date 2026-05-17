using DevStart.Domain.Experts;

namespace DevStart.Application.ExpertProfiles.GetAll
{
    public sealed class ExpertCatalogResponse
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string DisplayName { get; init; } = null!;
        public string? Bio { get; init; }
        public string? Website { get; init; }
        public string? LinkedInUrl { get; init; }
        public string? TwitterUrl { get; init; }
        public string? GitHubUrl { get; init; }
        public string? TelegramUrl { get; init; }
        public List<ExpertSpecialization> Specializations { get; init; } = new();
        public int ExperiencesCount { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
