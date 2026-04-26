namespace DevStart.Application.StartupCompetitors.GetAllByStartupId
{
    public sealed class StartupCompetitorResponse
    {
        public Guid Id { get; init; }
        public Guid StartupId { get; init; }
        public string Name { get; init; } = null!;
        public string? Website { get; init; }
        public string? Description { get; init; }
        public string? StrengthsVsUs { get; init; }
        public string? WeaknessesVsUs { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
}
