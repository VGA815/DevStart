namespace DevStart.Application.Dashboard.GetPublicStats
{
    public sealed class PublicStatsResponse
    {
        public int StartupsCount { get; init; }
        public int InvestorsCount { get; init; }
        public int ExpertsCount { get; init; }
        public decimal TotalRaised { get; init; }
    }
}
