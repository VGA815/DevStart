using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupCompetitors.Update
{
    public sealed class UpdateStartupCompetitorCommand : ICommand
    {
        public Guid CompetitorId { get; set; }
        public string Name { get; set; } = null!;
        public string? Website { get; set; }
        public string? Description { get; set; }
        public string? StrengthsVsUs { get; set; }
        public string? WeaknessesVsUs { get; set; }

        public UpdateStartupCompetitorCommand(Guid competitorId, string name, string? website, string? description,
            string? strengthsVsUs, string? weaknessesVsUs)
        {
            CompetitorId = competitorId;
            Name = name;
            Website = website;
            Description = description;
            StrengthsVsUs = strengthsVsUs;
            WeaknessesVsUs = weaknessesVsUs;
        }
    }
}
