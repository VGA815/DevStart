using DevStart.Application.Abstractions.Validation;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Application.Scoring;
using DevStart.Application.StartupEquity;
using DevStart.Domain.InvestmentApplications;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.StartupEquity;
using DevStart.Domain.Startups;

namespace DevStart.UnitTests.Application.DealDocuments;

/// <summary>
/// The three inputs the golden test renders. One per template, chosen so that between them every
/// branch in the markdown renderer is taken: optional terms present and absent, founders with and
/// without an explicit vesting schedule, no founders at all, warnings present and absent, and a
/// scoring result that is both available and unavailable.
/// <para>
/// Everything is fixed — ids, dates, amounts — because the fixtures compare byte for byte.
/// </para>
/// </summary>
internal static class TermSheetSamples
{
    internal static readonly DateTime AsOf = new(2026, 5, 17, 12, 0, 0, DateTimeKind.Utc);

    internal sealed record Sample(
        string Name,
        InvestmentDeal Deal,
        Startup Startup,
        ScoreResult Score,
        CapTableResult CapTable,
        IReadOnlyList<FoundingCapTableHolder> FoundingHolders);

    internal static IReadOnlyList<Sample> All => [Safe(), Convertible(), Priced()];

    /// <summary>SAFE: priced-round terms absent, mixed founder schedules, warnings raised.</summary>
    internal static Sample Safe() => new(
        "safe",
        new InvestmentDeal
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ApplicationId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"),
            InvestorProfileId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"),
            StartupId = Guid.Parse("cccccccc-1111-1111-1111-111111111111"),
            Amount = 1_500_000m,
            Instrument = InvestmentInstrument.Safe,
            ValuationCap = 60_000_000m,
            Discount = 0.2m,
            InterestRate = null,
            TermMonths = null,
            PreMoneyValuation = null,
            LiquidationPreference = 1.5m,
            ProRataRights = true,
        },
        new Startup
        {
            Id = Guid.Parse("cccccccc-1111-1111-1111-111111111111"),
            Name = "Кофейня на Луне",
            PublicEmail = "hello@luna.example",
            Stage = StartupStage.Mvp,
        },
        new ScoreResult(
            TotalScore: 72.4567m,
            TeamScore: 80m,
            MarketScore: 65.5m,
            ProductScore: 70m,
            TractionScore: 61.25m,
            CompetitionScore: 55m,
            ValuationLow: 48_000_000m,
            ValuationHigh: 72_000_000m,
            MethodsUsed: ["Berkus", "Scorecard"],
            CalculatedAt: new DateTime(2026, 5, 17, 9, 30, 0, DateTimeKind.Utc),
            ValuationPoint: 60_000_000m,
            ValuationMethods: null,
            MethodologyVersion: "2026.05"),
        new CapTableResult(
            [
                new CapTableEntry(Guid.Parse("dddddddd-1111-1111-1111-111111111111"), "Анна Петрова", "Founder", 60m, 52.5m, 39.375m),
                new CapTableEntry(Guid.Parse("dddddddd-2222-2222-2222-222222222222"), "Иван Смирнов", "Founder", 30m, 26.25m, 26.25m),
                new CapTableEntry(null, "ESOP", "Esop", 10m, 8.75m, 8.75m),
                new CapTableEntry(Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"), "Инвестор", "Investor", 0m, 12.5m, 12.5m),
            ],
            InvestorSharePct: 12.5m,
            FoundersTotalAfterPct: 78.75m,
            [
                new DealTermsFlag("VALUATION_CAP_BELOW_RANGE", "Warning", "Valuation cap is below the computed range."),
                new DealTermsFlag("HIGH_LIQUIDATION_PREFERENCE", "Info", "Liquidation preference above 1x."),
            ]),
        [
            new FoundingCapTableHolder(
                Guid.Parse("dddddddd-1111-1111-1111-111111111111"), EquityHolderType.Founder, "Анна Петрова", 60m,
                new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Utc), 48, 12),
            new FoundingCapTableHolder(
                Guid.Parse("dddddddd-2222-2222-2222-222222222222"), EquityHolderType.Founder, "Иван Смирнов", 30m,
                null, null, null),
            new FoundingCapTableHolder(null, EquityHolderType.Esop, "ESOP", 10m, null, null, null),
        ]);

    /// <summary>Convertible loan: every optional term set, no warnings, and no founders on record.</summary>
    internal static Sample Convertible() => new(
        "convertible",
        new InvestmentDeal
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ApplicationId = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"),
            InvestorProfileId = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"),
            StartupId = Guid.Parse("cccccccc-2222-2222-2222-222222222222"),
            Amount = 3_250_000.75m,
            Instrument = InvestmentInstrument.ConvertibleLoan,
            ValuationCap = 90_000_000m,
            Discount = 0.15m,
            InterestRate = 0.085m,
            TermMonths = 18,
            PreMoneyValuation = null,
            LiquidationPreference = 1m,
            ProRataRights = false,
        },
        new Startup
        {
            Id = Guid.Parse("cccccccc-2222-2222-2222-222222222222"),
            Name = "LogiFlow",
            PublicEmail = "hello@logiflow.example",
            Stage = StartupStage.Seed,
        },
        new ScoreResult(
            TotalScore: 58m,
            TeamScore: 62m,
            MarketScore: 51m,
            ProductScore: 60m,
            TractionScore: 55m,
            CompetitionScore: null,
            ValuationLow: 30_000_000m,
            ValuationHigh: 45_000_000m,
            MethodsUsed: ["Scorecard", "VC"],
            CalculatedAt: new DateTime(2026, 5, 16, 18, 5, 0, DateTimeKind.Utc),
            ValuationPoint: 37_500_000m,
            ValuationMethods: null,
            MethodologyVersion: ""),
        new CapTableResult(
            [
                new CapTableEntry(null, "ESOP", "Esop", 15m, 13.5m, 13.5m),
                new CapTableEntry(Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"), "Инвестор", "Investor", 0m, 10m, 10m),
            ],
            InvestorSharePct: 10m,
            FoundersTotalAfterPct: 76.5m,
            []),
        []);

    /// <summary>Priced round: scoring produced nothing usable — the whole score block reads N/A.</summary>
    internal static Sample Priced() => new(
        "priced",
        new InvestmentDeal
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ApplicationId = Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333"),
            InvestorProfileId = Guid.Parse("bbbbbbbb-3333-3333-3333-333333333333"),
            StartupId = Guid.Parse("cccccccc-3333-3333-3333-333333333333"),
            Amount = 25_000_000m,
            Instrument = InvestmentInstrument.PricedRound,
            ValuationCap = null,
            Discount = null,
            InterestRate = null,
            TermMonths = null,
            PreMoneyValuation = 200_000_000m,
            LiquidationPreference = 1m,
            ProRataRights = true,
        },
        new Startup
        {
            Id = Guid.Parse("cccccccc-3333-3333-3333-333333333333"),
            Name = "МедТех Сибирь",
            PublicEmail = "hello@medtech.example",
            Stage = StartupStage.SeriesA,
        },
        ScoreResult.InsufficientData(new DateTime(2026, 5, 17, 11, 0, 0, DateTimeKind.Utc)),
        new CapTableResult(
            [
                new CapTableEntry(Guid.Parse("dddddddd-3333-3333-3333-333333333333"), "Ольга Кузнецова", "Founder", 100m, 88.888m, 44.444m),
                new CapTableEntry(Guid.Parse("bbbbbbbb-3333-3333-3333-333333333333"), "Инвестор", "Investor", 0m, 11.111m, 11.111m),
            ],
            InvestorSharePct: 11.111m,
            FoundersTotalAfterPct: 88.888m,
            [new DealTermsFlag("NO_SCORE", "Warning", "Scoring produced no result for this startup.")]),
        [
            new FoundingCapTableHolder(
                Guid.Parse("dddddddd-3333-3333-3333-333333333333"), EquityHolderType.Founder, "Ольга Кузнецова", 100m,
                new DateTime(2025, 5, 17, 0, 0, 0, DateTimeKind.Utc), 24, 0),
        ]);
}
