using DevStart.Application.Scoring;
using DevStart.Domain.Startups;

namespace DevStart.UnitTests.Application.ScoringReports;

internal static class ScoringReportSamples
{
    internal static readonly DateTime Now = new(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc);
    internal static readonly DateTime CalculatedAt = new(2026, 5, 17, 9, 30, 0, DateTimeKind.Utc);
    internal static readonly Guid StartupId = Guid.Parse("cccccccc-1111-1111-1111-111111111111");

    internal static Startup Startup() => new()
    {
        Id = StartupId,
        Name = "Кофейня на Луне",
        PublicEmail = "hello@luna.example",
        Stage = StartupStage.Mvp,
    };

    /// <summary>
    /// A complete score whose competition factor dropped out — the case the report has to state as
    /// "no data" with a redistributed weight rather than as a zero.
    /// </summary>
    internal static ScoreResult Score() => new(
        TotalScore: 72.4567m,
        TeamScore: 80m,
        MarketScore: 65.5m,
        ProductScore: 70m,
        TractionScore: 61.25m,
        CompetitionScore: null,
        ValuationLow: 48_000_000m,
        ValuationHigh: 72_000_000m,
        MethodsUsed: ["Berkus", "Scorecard"],
        CalculatedAt: CalculatedAt,
        ValuationPoint: 60_000_000m,
        ValuationMethods: null,
        MethodologyVersion: "2026.05")
    {
        Factors =
        [
            new ScoreFactorBreakdown("Team", 80m, 0.30m, ScoreFactorSource.SelfReported),
            new ScoreFactorBreakdown("Market", 65.5m, 0.25m, ScoreFactorSource.SelfReported | ScoreFactorSource.ExternalBenchmark),
            new ScoreFactorBreakdown("Product", 70m, 0.25m,
                ScoreFactorSource.SelfReported | ScoreFactorSource.PlatformDerived | ScoreFactorSource.RegistryChecked),
            new ScoreFactorBreakdown("Traction", 61.25m, 0.20m, ScoreFactorSource.SelfReported),
            new ScoreFactorBreakdown("Competition", null, 0m, ScoreFactorSource.None),
        ]
    };

    internal static ScoreResult NoData() => ScoreResult.InsufficientData(CalculatedAt);
}
