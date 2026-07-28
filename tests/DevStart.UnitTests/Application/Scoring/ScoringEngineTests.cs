using DevStart.Application;
using DevStart.Application.Scoring;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Scoring;

public sealed class ScoringEngineTests
{
    private static readonly DateTime CalculatedAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

    private readonly IScoringEngine _scoringEngine = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IScoringEngine>();

    [Fact]
    public void Compute_ShouldReturnZeroScores_WhenInputsAreEmpty()
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(StartupStage.Idea, competitors: new CompetitorSignals(TotalCount: 4, WellDocumentedCount: 0)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        // Idea-stage weights: product 0.20, competition 0.10. Four cards with no analysis carry no
        // documentation bonus, so competition sits at the neutral 50 → 15*0.20 + 50*0.10 = 8.0
        result.TotalScore.ShouldBe(8.0m);
        result.TeamScore.ShouldBe(0m);
        result.MarketScore.ShouldBe(0m);
        result.ProductScore.ShouldBe(15m);
        result.TractionScore.ShouldBe(0m);
        result.CompetitionScore.ShouldBe(50m);
        result.ValuationLow.ShouldBe(0m);
        result.ValuationHigh.ShouldBe(0m);
        result.MethodsUsed.ShouldBeEmpty();
        result.CalculatedAt.ShouldBe(CalculatedAt);
    }

    [Fact]
    public void Compute_ShouldApplyTeamMarketProductTractionAndCompetitionRules()
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(
                StartupStage.Mvp,
                tam: 10_000_000_000m,
                cagr: 20m,
                hasPatents: true,
                competitors: new CompetitorSignals(TotalCount: 3, WellDocumentedCount: 3),
                members:
                [
                    new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 4, false, 0),
                    new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CTO, 1, false, 0),
                    new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CMO, 1, false, 0)
                ],
                traction: new TractionSignals(4_000_000m, 0m, 20m)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        result.TeamScore.ShouldBe(75m);
        result.MarketScore.ShouldBe(100m);
        result.ProductScore.ShouldBe(70m);
        result.TractionScore.ShouldBe(95m);
        // Neutral base 50 + three documented cards (saturated bonus +30).
        result.CompetitionScore.ShouldBe(80m);
        // Mvp-stage weights: 75*.25 + 100*.25 + 70*.15 + 95*.25 + 80*.10 = 86.0
        result.TotalScore.ShouldBe(86.0m);
    }

    [Fact]
    public void Compute_ShouldUseHighestFounderTierAndRoundTotal()
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(
                StartupStage.Seed,
                tam: 1_000_000_000m,
                cagr: 10m,
                competitors: new CompetitorSignals(TotalCount: 2, WellDocumentedCount: 1),
                members:
                [
                    new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 1, false, 0),
                    new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CTO, 1, true, 0),
                    new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CMO, null, null, null)
                ],
                traction: new TractionSignals(100_000m, 0m, 10m)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        result.TeamScore.ShouldBe(100m);
        result.MarketScore.ShouldBe(70m);
        result.ProductScore.ShouldBe(75m);
        result.TractionScore.ShouldBe(70m);
        result.CompetitionScore.ShouldBe(60m);
        // Seed-stage weights: 100*.25 + 70*.25 + 75*.15 + 70*.25 + 60*.10 = 77.25
        result.TotalScore.ShouldBe(77.25m);
    }

    [Fact]
    public void Compute_ShouldScoreMauOnlyTractionWithoutRevenue()
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(
                StartupStage.PreSeed,
                tam: 500_000_000m,
                cagr: 5m,
                competitors: new CompetitorSignals(TotalCount: 3, WellDocumentedCount: 1),
                members:
                [
                    new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 2, false, 0)
                ],
                traction: new TractionSignals(0m, 1_000m, 0m)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        result.MarketScore.ShouldBe(20m);
        result.TractionScore.ShouldBe(35m);
        result.CompetitionScore.ShouldBe(60m);
    }

    [Fact]
    public void Compute_ShouldScoreDecliningRevenueBelowFlatGrowth()
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(StartupStage.Seed, traction: new TractionSignals(2_000_000m, 0m, -5m)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        // MRR > 0 but shrinking → decline tier (25), below the flat-growth 50.
        result.TractionScore.ShouldBe(25m);
    }

    [Fact]
    public void Compute_ShouldUseAllMembersForExperience_WhenNoFounderFlagged()
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(StartupStage.Seed, members:
            [
                // No member is flagged Founder, but one is a serial entrepreneur with an exit.
                new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CEO, 6, true, 2)
            ]),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        // Falls back to the highest tier among all members (SerialWithExit = 90), not NoExperience (30).
        result.TeamScore.ShouldBe(90m);
    }

    [Fact]
    public void Compute_ShouldRewardConsistentMarketFunnel()
    {
        // Tam 1B → From1To10B base 60, no CAGR. Consistent funnel (0 < Som <= Sam <= Tam) → +5.
        ScoreResult consistent = _scoringEngine.Compute(
            Inputs(StartupStage.Seed, tam: 1_000_000_000m, sam: 500_000_000m, som: 100_000_000m),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);
        consistent.MarketScore.ShouldBe(65m);

        // Inconsistent funnel (Som > Sam) → no bonus.
        ScoreResult inconsistent = _scoringEngine.Compute(
            Inputs(StartupStage.Seed, tam: 1_000_000_000m, sam: 100_000_000m, som: 500_000_000m),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);
        inconsistent.MarketScore.ShouldBe(60m);
    }

    [Fact]
    public void Compute_ShouldRewardArticulatedPositioningAndPlanning()
    {
        // Idea base 15, +5 articulated positioning, +5 for >= 3 roadmap items.
        ScoreResult result = _scoringEngine.Compute(
            Inputs(
                StartupStage.Idea,
                product: new ProductSignals(HasArticulatedPositioning: true),
                roadmap: new RoadmapSignals(ItemCount: 3, DoneCount: 1)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        result.ProductScore.ShouldBe(25m);
    }

    // ---- Competition factor: the number of cards is not a driver (SC-36) ----------------------

    [Fact]
    public void Compute_ShouldExcludeCompetition_WhenNoCardsAndNoBenchmark()
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(StartupStage.Idea, competitors: CompetitorSignals.None),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        // The old engine handed an empty list the top of the scale (85). Now it is "no data".
        result.CompetitionScore.ShouldBeNull();

        ScoreFactorBreakdown competition = result.Factors.Single(f => f.Factor == "Competition");
        competition.Score.ShouldBeNull();
        competition.Weight.ShouldBe(0m);
        competition.Source.ShouldBe(ScoreFactorSource.None);

        // The remaining four weights are renormalized to exactly 1.0.
        result.Factors.Sum(f => f.Weight).ShouldBe(1.0m);
        result.Factors.Count(f => f.Score.HasValue).ShouldBe(4);
    }

    [Fact]
    public void Compute_FiveUndocumentedCards_ShouldScoreBelowTwoDocumentedOnes()
    {
        ScoreResult fiveEmpty = _scoringEngine.Compute(
            StrongStartup(new CompetitorSignals(TotalCount: 5, WellDocumentedCount: 0)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        ScoreResult twoDocumented = _scoringEngine.Compute(
            StrongStartup(new CompetitorSignals(TotalCount: 2, WellDocumentedCount: 2)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        fiveEmpty.CompetitionScore.ShouldBe(50m);
        twoDocumented.CompetitionScore.ShouldBe(70m);
        fiveEmpty.TotalScore!.Value.ShouldBeLessThan(twoDocumented.TotalScore!.Value);
    }

    [Fact]
    public void Compute_ShouldIgnoreTheNumberOfCards_AndCountOnlyDocumentedOnes()
    {
        ScoreResult oneOfTen = _scoringEngine.Compute(
            StrongStartup(new CompetitorSignals(TotalCount: 10, WellDocumentedCount: 1)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        ScoreResult oneOfOne = _scoringEngine.Compute(
            StrongStartup(new CompetitorSignals(TotalCount: 1, WellDocumentedCount: 1)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        // Listing nine more competitors you have not analysed neither helps nor hurts: the honest
        // founder is not punished for a longer list.
        oneOfTen.CompetitionScore.ShouldBe(oneOfOne.CompetitionScore);
        oneOfTen.TotalScore.ShouldBe(oneOfOne.TotalScore);
    }

    [Theory]
    // Deleting a documented card (bonus falls) — inside the participating regime.
    [InlineData(3, 3, 2, 2)]
    [InlineData(2, 2, 1, 1)]
    // Deleting an undocumented card — no change either way.
    [InlineData(5, 2, 4, 2)]
    // Deleting the last card, so the factor drops out entirely — the boundary the ceiling rule covers.
    [InlineData(1, 1, 0, 0)]
    [InlineData(1, 0, 0, 0)]
    public void Compute_DeletingACard_ShouldNeverRaiseTheTotal(
        int beforeTotal, int beforeDocumented, int afterTotal, int afterDocumented)
    {
        ScoreResult before = _scoringEngine.Compute(
            StrongStartup(new CompetitorSignals(beforeTotal, beforeDocumented)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        ScoreResult after = _scoringEngine.Compute(
            StrongStartup(new CompetitorSignals(afterTotal, afterDocumented)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        after.TotalScore!.Value.ShouldBeLessThanOrEqualTo(before.TotalScore!.Value);
    }

    [Fact]
    public void Compute_ShouldCapTheDroppedOutFactor_AtItsFloor()
    {
        // A strong startup: renormalizing the missing competition weight over the other four would
        // hand "no data" ~97.8, above anything the factor can actually produce. The ceiling rule caps
        // the total at what an unanalysed list (the factor's floor, 50) would have produced.
        ScoreResult noCards = _scoringEngine.Compute(
            StrongStartup(CompetitorSignals.None), ValuationBenchmarkSet.Empty, CalculatedAt);
        ScoreResult oneEmptyCard = _scoringEngine.Compute(
            StrongStartup(new CompetitorSignals(1, 0)), ValuationBenchmarkSet.Empty, CalculatedAt);

        // 100*.25 + 100*.25 + 95*.15 + 95*.25 = 88.0, plus the floor 50*.10 → 93.0
        noCards.TotalScore.ShouldBe(93.0m);
        oneEmptyCard.TotalScore.ShouldBe(93.0m);
    }

    // ---- Competition factor: the external sector benchmark (SC-37) ----------------------------

    [Fact]
    public void Compute_ShouldReadCompetitionIntensityFromTheBenchmark()
    {
        ValuationBenchmarkSet crowded = Benchmarks(Industry.Saas, intensity: 80m);

        ScoreResult noCards = _scoringEngine.Compute(
            StrongStartup(CompetitorSignals.None, Industry.Saas), crowded, CalculatedAt);

        // The factor participates on the benchmark alone: 100 − 80 = 20, no documentation bonus.
        noCards.CompetitionScore.ShouldBe(20m);
        noCards.Factors.Single(f => f.Factor == "Competition").Source
            .ShouldBe(ScoreFactorSource.ExternalBenchmark);

        ScoreResult documented = _scoringEngine.Compute(
            StrongStartup(new CompetitorSignals(2, 2), Industry.Saas), crowded, CalculatedAt);

        documented.CompetitionScore.ShouldBe(40m);
        documented.Factors.Single(f => f.Factor == "Competition").Source
            .ShouldBe(ScoreFactorSource.ExternalBenchmark | ScoreFactorSource.SelfReported);
    }

    [Fact]
    public void Compute_ShouldFallBackToTheGeneralIntensityRow_ForOtherSectors()
    {
        ValuationBenchmarkSet general = Benchmarks(Industry.Other, intensity: 30m);

        ScoreResult result = _scoringEngine.Compute(
            StrongStartup(CompetitorSignals.None, Industry.Fintech), general, CalculatedAt);

        result.CompetitionScore.ShouldBe(70m);
    }

    // ---- Weights and the insufficient-data outcome (SC-35) ------------------------------------

    [Theory]
    [InlineData(StartupStage.Idea)]
    [InlineData(StartupStage.PreSeed)]
    [InlineData(StartupStage.Mvp)]
    [InlineData(StartupStage.Seed)]
    [InlineData(StartupStage.SeriesA)]
    public void WeightsFor_ShouldSumToOne(StartupStage stage)
    {
        ScoringEngine.ScoreWeights w = ScoringEngine.WeightsFor(stage);

        (w.Team + w.Market + w.Product + w.Traction + w.Competition).ShouldBe(1.00m);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Combine_ShouldRenormalizeWeightsToOne_OverAnySubsetOfFactors(int absentCount)
    {
        ScoringEngine.ScoreFactor[] factors =
        [
            Factor("Team", 60m, 0.25m, absentCount >= 4),
            Factor("Market", 60m, 0.25m, absentCount >= 3),
            Factor("Product", 60m, 0.15m, absentCount >= 2),
            Factor("Traction", 60m, 0.25m, absentCount >= 1),
            Factor("Competition", 60m, 0.10m, absent: false),
        ];

        ScoreResult result = ScoringEngine.Combine(factors, CalculatedAt);

        result.Factors.Sum(f => f.Weight).ShouldBe(1.0m);
        result.Factors.Count(f => f.Score.HasValue).ShouldBe(5 - absentCount);
        // Every participating factor scores 60, so the renormalized total is 60 whatever dropped out.
        result.TotalScore.ShouldBe(60m);
    }

    [Fact]
    public void Combine_ShouldReturnInsufficientData_WhenNoFactorHasData()
    {
        ScoringEngine.ScoreFactor[] factors =
        [
            Factor("Team", 60m, 0.25m, absent: true),
            Factor("Market", 60m, 0.25m, absent: true),
            Factor("Product", 60m, 0.15m, absent: true),
            Factor("Traction", 60m, 0.25m, absent: true),
            Factor("Competition", 60m, 0.10m, absent: true),
        ];

        ScoreResult result = ScoringEngine.Combine(factors, CalculatedAt);

        // Explicitly "not computable", not a 0 that reads like the worst possible startup.
        result.TotalScore.ShouldBeNull();
        result.CompetitionScore.ShouldBeNull();
        result.Factors.ShouldBeEmpty();
        result.CalculatedAt.ShouldBe(CalculatedAt);
    }

    // ---- Provenance (SC-39) -------------------------------------------------------------------

    [Fact]
    public void Compute_ShouldReportTheProvenanceOfEveryFactor()
    {
        ScoreResult empty = _scoringEngine.Compute(
            Inputs(StartupStage.Idea), ValuationBenchmarkSet.Empty, CalculatedAt);

        // An empty profile still scores, but every factor is flagged as resting on nothing.
        empty.Factors.Single(f => f.Factor == "Team").Source.ShouldBe(ScoreFactorSource.None);
        empty.Factors.Single(f => f.Factor == "Market").Source.ShouldBe(ScoreFactorSource.None);
        empty.Factors.Single(f => f.Factor == "Traction").Source.ShouldBe(ScoreFactorSource.None);
        empty.Factors.Single(f => f.Factor == "Competition").Source.ShouldBe(ScoreFactorSource.None);
        empty.Factors.Single(f => f.Factor == "Product").Source
            .ShouldBe(ScoreFactorSource.SelfReported | ScoreFactorSource.PlatformDerived);

        ScoreResult filled = _scoringEngine.Compute(
            Inputs(
                StartupStage.Seed,
                tam: 1_000_000_000m,
                members: [new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 5, false, 0)],
                traction: TractionSignals.From(100_000m, 0m, 5m)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        filled.Factors.Single(f => f.Factor == "Team").Source.ShouldBe(ScoreFactorSource.SelfReported);
        filled.Factors.Single(f => f.Factor == "Market").Source.ShouldBe(ScoreFactorSource.SelfReported);
        filled.Factors.Single(f => f.Factor == "Traction").Source.ShouldBe(ScoreFactorSource.SelfReported);
    }

    // ---- Helpers ------------------------------------------------------------------------------

    private static ScoringEngine.ScoreFactor Factor(string name, decimal score, decimal weight, bool absent) =>
        new(name, absent ? null : score, weight, ScoreFactorSource.SelfReported, ScoreFactorDetail.Empty);

    private static ValuationBenchmarkSet Benchmarks(Industry industry, decimal intensity) =>
        ValuationBenchmarkSet.FromRows(
            [new ValuationBenchmarkRow(
                BenchmarkMetricType.CompetitionIntensity, industry, null, intensity,
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))],
            CalculatedAt);

    // A Seed startup that scores 100/100/95/95 on the other four factors: the case where the
    // renormalized "no data" outcome would otherwise beat every real competition score.
    private static ScoringInputs StrongStartup(CompetitorSignals competitors, Industry industry = Industry.Other) =>
        Inputs(
            StartupStage.Seed,
            tam: 10_000_000_000m,
            cagr: 20m,
            hasPatents: true,
            competitors: competitors,
            members:
            [
                new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 8, true, 2),
                new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CTO, 5, false, 0),
                new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CMO, 5, false, 0)
            ],
            traction: new TractionSignals(4_000_000m, 0m, 20m),
            product: new ProductSignals(HasArticulatedPositioning: true),
            roadmap: new RoadmapSignals(ItemCount: 4, DoneCount: 2),
            industry: industry);

    private static ScoringInputs Inputs(
        StartupStage stage,
        IReadOnlyList<MemberInput>? members = null,
        TractionSignals? traction = null,
        decimal? tam = null,
        decimal? sam = null,
        decimal? som = null,
        decimal? cagr = null,
        bool hasPatents = false,
        CompetitorSignals? competitors = null,
        ProductSignals? product = null,
        RoadmapSignals? roadmap = null,
        Industry industry = Industry.Other) =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            stage,
            Tam: tam,
            Sam: sam,
            Som: som,
            MarketGrowthRate: cagr,
            HasPatents: hasPatents,
            Competitors: competitors ?? CompetitorSignals.None,
            Members: members ?? [],
            Traction: traction ?? TractionSignals.Empty,
            Product: product ?? ProductSignals.None,
            Roadmap: roadmap ?? RoadmapSignals.None,
            Industry: industry);
}
