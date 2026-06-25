namespace DevStart.Application.Scoring
{
    public sealed record ScoreResult(
        decimal TotalScore,
        decimal TeamScore,
        decimal MarketScore,
        decimal ProductScore,
        decimal TractionScore,
        decimal CompetitionScore,
        decimal ValuationLow,
        decimal ValuationHigh,
        IReadOnlyList<string> MethodsUsed,
        DateTime CalculatedAt,
        decimal ValuationPoint = 0m,
        IReadOnlyList<ValuationBreakdown>? ValuationMethods = null,
        string MethodologyVersion = "");
}
