using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupCompetitors.Create
{
    public sealed class CreateStartupCompetitorCommand : ICommand<Guid>
    {
        public Guid StartupId { get; set; }
        public string Name { get; set; } = null!;
        public string? Website { get; set; }
        public string? Description { get; set; }
        public string? StrengthsVsUs { get; set; }
        public string? WeaknessesVsUs { get; set; }

        public CreateStartupCompetitorCommand(Guid startupId, string name, string? website, string? description,
            string? strengthsVsUs, string? weaknessesVsUs)
        {
            StartupId = startupId;
            Name = name;
            Website = website;
            Description = description;
            StrengthsVsUs = strengthsVsUs;
            WeaknessesVsUs = weaknessesVsUs;
        }
    }
}
