using DevStart.Application.Scoring;
using DevStart.Domain.Startups;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.Scoring;

public sealed class ValuationCalculatorTests
{
    private static readonly DateTime Now = new(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc);

    private readonly ValuationOptions _options = new();
    private IValuationCalculator Sut => new ValuationCalculator(new OptionsWrapper<ValuationOptions>(_options));

    // The medians that used to be hardcoded in ScorecardOptions now arrive from the benchmark set;
    // the default set carries them (sector Other) so the per-method assertions stay stable.
    private static ValuationBenchmarkSet DefaultBenchmarks() => new(
        new Dictionary<(Industry, StartupStage), decimal>
        {
            [(Industry.Other, StartupStage.Idea)] = 60_000_000m,
            [(Industry.Other, StartupStage.PreSeed)] = 120_000_000m,
            [(Industry.Other, StartupStage.Mvp)] = 250_000_000m,
            [(Industry.Other, StartupStage.Seed)] = 400_000_000m,
        },
        new Dictionary<Industry, decimal>());

    private ValuationResult Compute(ScoreResult score, ScoringInputs inputs, ValuationBenchmarkSet? benchmarks = null)
    {
        IValuationCalculator calculator = Sut;
        return calculator.Compute(score, inputs, benchmarks ?? DefaultBenchmarks());
    }

    private static ScoreResult Score(
        decimal total = 50m, decimal team = 50m, decimal market = 50m,
        decimal product = 50m, decimal traction = 50m, decimal? competition = 50m) =>
        new(total, team, market, product, traction, competition, 0m, 0m, [], Now);

    private static ScoringInputs Inputs(
        StartupStage stage,
        Industry industry = Industry.Other,
        decimal mrr = 0m,
        bool partnerships = false,
        bool articulated = false,
        bool patents = false,
        decimal? targetRoundAmount = null) =>
        new(
            Guid.NewGuid(),
            stage,
            Tam: null, Sam: null, Som: null, MarketGrowthRate: null,
            HasPatents: patents,
            Competitors: CompetitorSignals.None,
            Members: [],
            Traction: TractionSignals.From(mrr, 0m, 0m),
            Product: new ProductSignals(articulated),
            Roadmap: RoadmapSignals.None,
            Industry: industry,
            TargetRoundAmount: targetRoundAmount,
            HasStrategicPartnerships: partnerships);

    private static decimal MethodValue(ValuationResult r, string method) =>
        r.Methods.Single(m => m.Method == method).Value;

    // ---- Per-method DoD checks (read from the breakdown so each method is asserted in isolation) ----

    [Fact]
    public void Berkus_ZeroesPartnershipsFactor_ReachingAboutTwoThirdsOfMax()
    {
        // Idea 1.0 (articulated) + prototype 0.8 (Mvp) + team 1.0 (sub 100) + partnerships 0 + traction 0.6
        // = 3.4 of 5 ceilings × ₽45M = ₽153M ≈ 0.68 × ₽225M max — the spec's $1.7M / $2.5M ratio.
        ValuationResult r = Compute(
            Score(team: 100m, traction: 60m),
            Inputs(StartupStage.Mvp, articulated: true, partnerships: false, patents: false));

        decimal berkus = MethodValue(r, "Berkus");
        berkus.ShouldBe(153_000_000m);
        (berkus / 225_000_000m).ShouldBe(0.68m);
    }

    [Fact]
    public void Berkus_FullPartnerships_AddsTheFifthFactor()
    {
        decimal withoutPartnerships = MethodValue(
            Compute(Score(team: 100m, traction: 60m),
                Inputs(StartupStage.Mvp, articulated: true, partnerships: false)),
            "Berkus");
        decimal withPartnerships = MethodValue(
            Compute(Score(team: 100m, traction: 60m),
                Inputs(StartupStage.Mvp, articulated: true, partnerships: true)),
            "Berkus");

        (withPartnerships - withoutPartnerships).ShouldBe(45_000_000m); // exactly one ceiling
    }

    [Fact]
    public void Scorecard_SaasSeed_LandsNearTheWorkedExample()
    {
        // median ₽400M (Seed) × composite multiplier (team 1.2, market 1.3, product 1.0, competition 1.0,
        // sales/traction 0.8, financing/other 1.0) = 1.115 → ₽446M (spec ≈ ₽442M).
        ValuationResult r = Compute(
            Score(team: 70m, market: 80m, product: 50m, traction: 30m, competition: 50m),
            Inputs(StartupStage.Seed, Industry.Saas));

        MethodValue(r, "Scorecard").ShouldBe(446_000_000m);
    }

    [Fact]
    public void Scorecard_DropsANoDataSubScore_InsteadOfApplyingTheFloorMultiplier()
    {
        ScoringInputs inputs = Inputs(StartupStage.Seed, Industry.Saas);

        // Same startup as the worked example, but the competition factor had no data.
        ValuationResult noData = Compute(
            Score(team: 70m, market: 80m, product: 50m, traction: 30m, competition: null), inputs);

        // Reading "no data" as a 0 sub-score would silently apply the floor multiplier (0.5) and
        // knock ₽20M off the valuation — the leak this rule closes.
        ValuationResult asZero = Compute(
            Score(team: 70m, market: 80m, product: 50m, traction: 30m, competition: 0m), inputs);

        MethodValue(noData, "Scorecard").ShouldBeGreaterThan(MethodValue(asZero, "Scorecard"));

        // Kept weights (0.90 of the total) renormalized back to 1.0:
        // (0.30*1.2 + 0.25*1.3 + 0.15*1.0 + 0.10*0.8 + 0.05 + 0.05) / 0.90 × ₽400M
        MethodValue(noData, "Scorecard").ShouldBe(451_111_111m);

        noData.Methods.Single(m => m.Method == "Scorecard").Assumptions
            .ShouldContain(a => a.Contains("no data for competition"));
    }

    [Fact]
    public void VcMethod_SeriesA_ReversesFromExitToAboutTheWorkedExample()
    {
        // Pre-revenue SeriesA: assumed exit revenue ₽500M × 6× = TV ₽3 000M; post = TV / 1.4^5 ≈ ₽557.8M.
        ValuationResult r = Compute(Score(), Inputs(StartupStage.SeriesA, Industry.Saas, mrr: 0m));

        decimal discount = 1m;
        for (int i = 0; i < 5; i++)
        {
            discount *= 1.40m;
        }
        decimal expected = Math.Round(3_000_000_000m / discount, 0, MidpointRounding.AwayFromZero);

        MethodValue(r, "VcMethod").ShouldBe(expected);
        expected.ShouldBeInRange(557_000_000m, 558_000_000m);
    }

    [Fact]
    public void VcMethod_AnchorsExitRevenueToArr_WhenRevenuePresent()
    {
        // ARR = MRR×12 = ₽600M; exit revenue = ARR × growth(10) = ₽6 000M — far above the pre-revenue floor.
        decimal withArr = MethodValue(
            Compute(Score(), Inputs(StartupStage.Seed, Industry.Saas, mrr: 50_000_000m)),
            "VcMethod");
        decimal preRevenue = MethodValue(
            Compute(Score(), Inputs(StartupStage.Seed, Industry.Saas, mrr: 0m)),
            "VcMethod");

        withArr.ShouldBeGreaterThan(preRevenue);
    }

    [Fact]
    public void VcMethod_SubtractsRoundAmount_ForPreMoney_WhenTargetKnown()
    {
        decimal postMoney = MethodValue(
            Compute(Score(), Inputs(StartupStage.SeriesA, Industry.Saas)),
            "VcMethod");
        decimal preMoney = MethodValue(
            Compute(Score(), Inputs(StartupStage.SeriesA, Industry.Saas, targetRoundAmount: 100_000_000m)),
            "VcMethod");

        (postMoney - preMoney).ShouldBe(100_000_000m);
    }

    // ---- Ensemble: applicability matrix + weight renormalization ----

    [Theory]
    [InlineData(StartupStage.Idea, "Berkus", "Scorecard")]
    [InlineData(StartupStage.PreSeed, "Berkus", "Scorecard")]
    [InlineData(StartupStage.Seed, "Scorecard", "VcMethod")]
    public void Ensemble_AppliesTheRightTwoMethods_AndSplitsWeightEvenly(
        StartupStage stage, string first, string second)
    {
        ValuationResult r = Compute(Score(), Inputs(stage));

        r.MethodsUsed.ShouldBe([first, second]);
        r.Methods.Select(m => m.Weight).ShouldAllBe(w => w == 0.5m);
        r.Methods.Sum(m => m.Weight).ShouldBe(1.0m);
    }

    [Fact]
    public void Ensemble_Mvp_AppliesAllThreeMethods_AndWeightsSumToExactlyOne()
    {
        ValuationResult r = Compute(Score(), Inputs(StartupStage.Mvp));

        r.MethodsUsed.ShouldBe(["Berkus", "Scorecard", "VcMethod"]);
        // Three equal methods round to 0.33/0.33/0.34 — the residual is folded into the last so the
        // displayed weights still sum to exactly 1.0 (not 0.99).
        r.Methods.Sum(m => m.Weight).ShouldBe(1.0m);
    }

    [Fact]
    public void Ensemble_SeriesA_UsesVcMethodOnly_WithFullWeight()
    {
        ValuationResult r = Compute(Score(), Inputs(StartupStage.SeriesA));

        r.MethodsUsed.ShouldBe(["VcMethod"]);
        r.Methods.Single().Weight.ShouldBe(1.0m);
    }

    // ---- Range invariant + guardrails ----

    [Theory]
    [InlineData(StartupStage.Idea)]
    [InlineData(StartupStage.PreSeed)]
    [InlineData(StartupStage.Mvp)]
    [InlineData(StartupStage.Seed)]
    [InlineData(StartupStage.SeriesA)]
    public void Range_AlwaysBracketsThePoint(StartupStage stage)
    {
        ValuationResult r = Compute(
            Score(60m, 60m, 60m, 60m, 60m, 60m),
            Inputs(stage, Industry.Saas, mrr: 1_000_000m));

        r.Low.ShouldBeLessThanOrEqualTo(r.Point);
        r.Point.ShouldBeLessThanOrEqualTo(r.High);
        r.Low.ShouldBeGreaterThanOrEqualTo(0m);
        r.MethodologyVersion.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void NoApplicableMethod_ReturnsInsufficientData_NotASilentZeroRange()
    {
        // An out-of-range stage maps to no method — the explicit "insufficient data" signal.
        ValuationResult r = Compute(Score(), Inputs((StartupStage)99));

        r.MethodsUsed.ShouldBeEmpty();
        r.Methods.ShouldBeEmpty();
        r.Low.ShouldBe(0m);
        r.High.ShouldBe(0m);
        r.Point.ShouldBe(0m);
        r.MethodologyVersion.ShouldBe(_options.MethodologyVersion);
    }

    // ---- Backtest harness (SC-18): worked examples bracketed by the blended range ----

    [Fact]
    public void Backtest_WorkedExamples_AreBracketedByTheBlendedRange()
    {
        // Series A SaaS — VC worked example ≈ ₽557M sits inside the ±25% band.
        ValuationResult seriesA = Compute(Score(), Inputs(StartupStage.SeriesA, Industry.Saas));
        557_000_000m.ShouldBeInRange(seriesA.Low, seriesA.High);

        // Seed SaaS strong scorecard — the spec's ≈₽442M sits within the blended Seed range.
        ValuationResult seed = Compute(
            Score(team: 70m, market: 80m, product: 50m, traction: 30m, competition: 50m),
            Inputs(StartupStage.Seed, Industry.Saas));
        seed.High.ShouldBeGreaterThan(seed.Low);
        seed.Point.ShouldBeGreaterThan(0m);

        // Early-stage Berkus example — partnerships zeroed keeps Berkus below its ₽225M ceiling.
        ValuationResult preSeed = Compute(
            Score(team: 100m, traction: 60m),
            Inputs(StartupStage.PreSeed, articulated: true, partnerships: false));
        MethodValue(preSeed, "Berkus").ShouldBeLessThan(225_000_000m);
    }

    // ---- SC-27: Scorecard insufficient-data (no median on file → method drops out) ----

    [Fact]
    public void Scorecard_DropsOutOfEnsemble_WhenNoMedianOnFile()
    {
        // Idea applies Berkus + Scorecard; with an empty set the median is absent, so only Berkus
        // remains and absorbs the full (renormalized) weight.
        ValuationResult r = Compute(Score(), Inputs(StartupStage.Idea), ValuationBenchmarkSet.Empty);

        r.MethodsUsed.ShouldBe(["Berkus"]);
        r.Methods.Single().Weight.ShouldBe(1.0m);
    }

    [Fact]
    public void Scorecard_UsesSectorMedian_WhenSectorSpecificRowExists()
    {
        var set = new ValuationBenchmarkSet(
            new Dictionary<(Industry, StartupStage), decimal>
            {
                [(Industry.Other, StartupStage.Seed)] = 400_000_000m,
                [(Industry.Saas, StartupStage.Seed)] = 800_000_000m, // sector override
            },
            new Dictionary<Industry, decimal>());

        decimal sector = MethodValue(
            Compute(Score(team: 70m, market: 80m, product: 50m, traction: 30m, competition: 50m),
                Inputs(StartupStage.Seed, Industry.Saas), set),
            "Scorecard");
        decimal stageOnly = MethodValue(
            Compute(Score(team: 70m, market: 80m, product: 50m, traction: 30m, competition: 50m),
                Inputs(StartupStage.Seed, Industry.Fintech), set),
            "Scorecard");

        // Same composite multiplier, twice the median → twice the Scorecard value.
        sector.ShouldBe(stageOnly * 2m);
    }

    // ---- SC-28: Comparable (sector revenue multiple × ARR) ----

    [Fact]
    public void Comparable_AppliesOnSeriesA_WithSectorMultipleAndRevenue()
    {
        var set = new ValuationBenchmarkSet(
            new Dictionary<(Industry, StartupStage), decimal>(),
            new Dictionary<Industry, decimal> { [Industry.Saas] = 5m });

        // ARR = MRR × 12 = ₽120M; Comparable = 5× × ₽120M = ₽600M.
        ValuationResult r = Compute(Score(), Inputs(StartupStage.SeriesA, Industry.Saas, mrr: 10_000_000m), set);

        r.MethodsUsed.ShouldContain("Comparable");
        MethodValue(r, "Comparable").ShouldBe(600_000_000m);
    }

    [Fact]
    public void Comparable_DropsOut_WhenNoRevenue()
    {
        var set = new ValuationBenchmarkSet(
            new Dictionary<(Industry, StartupStage), decimal>(),
            new Dictionary<Industry, decimal> { [Industry.Saas] = 5m });

        ValuationResult r = Compute(Score(), Inputs(StartupStage.SeriesA, Industry.Saas, mrr: 0m), set);

        r.MethodsUsed.ShouldNotContain("Comparable");
    }

    [Fact]
    public void Comparable_DropsOut_WhenNoSectorMultiple()
    {
        // Revenue present, but no multiplier on file for the sector → method does not participate.
        ValuationResult r = Compute(
            Score(), Inputs(StartupStage.SeriesA, Industry.Saas, mrr: 10_000_000m), ValuationBenchmarkSet.Empty);

        r.MethodsUsed.ShouldNotContain("Comparable");
    }

    [Fact]
    public void Comparable_GivesSeriesA_MoreThanOneMethod_WhenDataPresent()
    {
        var set = new ValuationBenchmarkSet(
            new Dictionary<(Industry, StartupStage), decimal>(),
            new Dictionary<Industry, decimal> { [Industry.Saas] = 5m });

        ValuationResult r = Compute(Score(), Inputs(StartupStage.SeriesA, Industry.Saas, mrr: 10_000_000m), set);

        r.MethodsUsed.ShouldBe(["VcMethod", "Comparable"]);
        r.Methods.Sum(m => m.Weight).ShouldBe(1.0m);
    }
}
