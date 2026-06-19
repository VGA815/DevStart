namespace DevStart.Application.Users.GetOverview
{
    public sealed record UserStatisticsResponse
    {
        public bool IsInvestor { get; init; }
        public bool IsExpert { get; init; }

        // Investor stats. CompletedDealsCount is public; TotalInvestedAmount is owner-only and is
        // redacted to null for non-owners by GetUserOverviewQueryHandler.
        public int CompletedDealsCount { get; init; }
        public decimal? TotalInvestedAmount { get; init; }

        // Expert stats.
        public int AcceptedCollaborationsCount { get; init; }
        public int ExperiencesCount { get; init; }
    }
}
