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

    /// <summary>
    /// The round the VC method needs to participate at all (М4). Declared by default so the ensemble
    /// tests below keep exercising the method; the tests about withholding it pass <c>null</c>.
    /// </summary>
    private const decimal DefaultRound = 100_000_000m;

    private static ScoringInputs Inputs(
        StartupStage stage,
        Industry industry = Industry.Other,
        decimal mrr = 0m,
        PartnershipSignals? partnerships = null,
        bool articulated = false,
        bool patents = false,
        decimal? targetRoundAmount = DefaultRound) =>
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
            Partnerships: partnerships ?? PartnershipSignals.None,
            Industry: industry,
            TargetRoundAmount: targetRoundAmount);

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
            Inputs(StartupStage.Mvp, articulated: true, patents: false));

        decimal berkus = MethodValue(r, "Berkus");
        berkus.ShouldBe(153_000_000m);
        (berkus / 225_000_000m).ShouldBe(0.68m);
    }

    /// <summary>
    /// М3: the fifth Berkus factor is a ladder, not a switch. It used to be one checkbox worth the
    /// whole ₽45M ceiling — the largest effect-per-keystroke left in the model. Now each worked-out
    /// partnership record is worth a third of it, and the fourth record is worth nothing.
    /// </summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 15_000_000)]
    [InlineData(2, 30_000_000)]
    [InlineData(3, 45_000_000)]
    [InlineData(4, 45_000_000)] // saturated
    public void Berkus_GradesPartnershipsByWorkedOutRecords_SaturatingAtThree(int workedOut, decimal expectedGain)
    {
        decimal none = MethodValue(
            Compute(Score(team: 100m, traction: 60m), Inputs(StartupStage.Mvp, articulated: true)),
            "Berkus");
        decimal withRecords = MethodValue(
            Compute(Score(team: 100m, traction: 60m),
                Inputs(StartupStage.Mvp, articulated: true,
                    partnerships: new PartnershipSignals(TotalCount: workedOut, WorkedOutCount: workedOut))),
            "Berkus");

        (withRecords - none).ShouldBe(expectedGain);
    }

    /// <summary>
    /// The total is carried for transparency and is not a driver — ten placeholder records with no
    /// account of the arrangement are worth exactly what none are.
    /// </summary>
    [Fact]
    public void Berkus_IgnoresPartnershipRecordsThatSayNothing()
    {
        decimal none = MethodValue(
            Compute(Score(team: 100m, traction: 60m), Inputs(StartupStage.Mvp, articulated: true)),
            "Berkus");
        decimal placeholders = MethodValue(
            Compute(Score(team: 100m, traction: 60m),
                Inputs(StartupStage.Mvp, articulated: true,
                    partnerships: new PartnershipSignals(TotalCount: 10, WorkedOutCount: 0))),
            "Berkus");

        placeholders.ShouldBe(none);
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
        // Pre-revenue SeriesA: assumed exit revenue ₽500M × 6× = TV ₽3 000M; post = TV / 1.4^5 ≈ ₽557.8M,
        // and pre-money is that less the declared round.
        ValuationResult r = Compute(Score(), Inputs(StartupStage.SeriesA, Industry.Saas, mrr: 0m));

        decimal discount = 1m;
        for (int i = 0; i < 5; i++)
        {
            discount *= 1.40m;
        }
        decimal postMoney = 3_000_000_000m / discount;
        decimal expected = Math.Round(postMoney - DefaultRound, 0, MidpointRounding.AwayFromZero);

        MethodValue(r, "VcMethod").ShouldBe(expected);
        (expected + DefaultRound).ShouldBeInRange(557_000_000m, 558_000_000m);
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
    public void VcMethod_SubtractsTheRound_SoALargerRoundMeansALowerPreMoney()
    {
        decimal small = MethodValue(
            Compute(Score(), Inputs(StartupStage.SeriesA, Industry.Saas, targetRoundAmount: 100_000_000m)),
            "VcMethod");
        decimal large = MethodValue(
            Compute(Score(), Inputs(StartupStage.SeriesA, Industry.Saas, targetRoundAmount: 300_000_000m)),
            "VcMethod");

        (small - large).ShouldBe(200_000_000m);
    }

    // ---- М4: no gain from withholding the round ----

    /// <summary>
    /// The inversion this closes: pre-money is post-money minus the round, so an empty field used to
    /// skip the subtraction and hand back the largest pre-money the method can produce. Leaving the
    /// field blank paid — the audit's criterion №1, an action in the UI that raises the result by
    /// hiding data. The method now drops out instead, by the same "no input → no component" rule that
    /// already governs Scorecard without a median and Comparable without a multiple.
    /// </summary>
    [Theory]
    [InlineData(StartupStage.Mvp)]
    [InlineData(StartupStage.Seed)]
    [InlineData(StartupStage.SeriesA)]
    public void VcMethod_DropsOutOfEnsemble_WhenNoRoundDeclared(StartupStage stage)
    {
        ValuationResult r = Compute(Score(), Inputs(stage, targetRoundAmount: null));

        r.MethodsUsed.ShouldNotContain("VcMethod");
    }

    [Theory]
    [InlineData(StartupStage.Mvp, 0)]
    [InlineData(StartupStage.Seed, 0)]
    [InlineData(StartupStage.SeriesA, 0)]
    [InlineData(StartupStage.Mvp, 10_000_000)]
    [InlineData(StartupStage.Seed, 10_000_000)]
    [InlineData(StartupStage.SeriesA, 10_000_000)]
    public void WithholdingTheRound_NeverRaisesTheValuation(StartupStage stage, int mrr)
    {
        ValuationResult declared = Compute(
            Score(), Inputs(stage, Industry.Saas, mrr: mrr, targetRoundAmount: 50_000_000m));
        ValuationResult withheld = Compute(
            Score(), Inputs(stage, Industry.Saas, mrr: mrr, targetRoundAmount: null));

        withheld.Point.ShouldBeLessThanOrEqualTo(declared.Point);
        withheld.High.ShouldBeLessThanOrEqualTo(declared.High);
    }

    /// <summary>
    /// A zero or negative round is not a declaration either — it is the same empty field with a
    /// character in it, and it must not buy the method back.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1_000_000)]
    public void VcMethod_DropsOut_WhenTheRoundIsNotPositive(int round)
    {
        ValuationResult r = Compute(Score(), Inputs(StartupStage.SeriesA, targetRoundAmount: round));

        r.MethodsUsed.ShouldNotContain("VcMethod");
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

        // Early-stage Berkus example — no partnership records keeps Berkus below its ₽225M ceiling.
        ValuationResult preSeed = Compute(
            Score(team: 100m, traction: 60m),
            Inputs(StartupStage.PreSeed, articulated: true));
        MethodValue(preSeed, "Berkus").ShouldBeLessThan(225_000_000m);
    }

    // ---- SC-27: Scorecard insufficient-data (no median on file → method drops out) ----

    [Fact]
    public void Scorecard_DropsOutOfEnsemble_WhenNoMedianOnFile()
    {
        // Idea applies Berkus + Scorecard; with an empty set the median is absent, so only Berkus
        // remains and absorbs the full (renormalized) weight. (VC does not apply at Idea at all.)
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
