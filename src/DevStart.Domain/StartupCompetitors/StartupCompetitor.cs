using DevStart.SharedKernel;

namespace DevStart.Domain.StartupCompetitors
{
    public sealed class StartupCompetitor : Entity
    {
        public Guid Id { get; set; }
        public Guid StartupId { get; set; }
        public string Name { get; set; } = null!;
        public string? Website { get; set; }
        public string? Description { get; set; }
        public string? StrengthsVsUs { get; set; }
        public string? WeaknessesVsUs { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public StartupCompetitor() { }

        public static StartupCompetitor Create(
            Guid startupId,
            string name,
            string? website,
            string? description,
            string? strengthsVsUs,
            string? weaknessesVsUs,
            DateTime createdAt)
            => new()
            {
                Id = Guid.NewGuid(),
                StartupId = startupId,
                Name = name,
                Website = website,
                Description = description,
                StrengthsVsUs = strengthsVsUs,
                WeaknessesVsUs = weaknessesVsUs,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

        public void Update(
            string name,
            string? website,
            string? description,
            string? strengthsVsUs,
            string? weaknessesVsUs,
            DateTime utcNow)
        {
            Name = name;
            Website = website;
            Description = description;
            StrengthsVsUs = strengthsVsUs;
            WeaknessesVsUs = weaknessesVsUs;
            UpdatedAt = utcNow;
        }
    }
}
