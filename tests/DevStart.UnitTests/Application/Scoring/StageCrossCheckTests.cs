using DevStart.Application;
using DevStart.Application.Scoring;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.Scoring;

/// <summary>
/// М4's second half: the declared stage is cross-checked against the metrics on file, and the outcome
/// is shown — never scored on. A Series A with an empty metrics tab is a signal addressed to the
/// investor reading the profile, not an input for the engine, so nothing is blocked and no number
/// moves. The invariance half of this file is the same guard <see cref="RegistryCheckInvarianceTests"/>
/// puts on the register check, and for the same reason: a numeric effect is the kind of thing that
/// arrives later by accident.
/// </summary>
public sealed class StageCrossCheckTests
{
    private static readonly DateTime CalculatedAt = new(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc);

    private readonly IScoringEngine _scoringEngine = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IScoringEngine>();

    private readonly IValuationCalculator _valuationCalculator =
        new ValuationCalculator(new OptionsWrapper<ValuationOptions>(new ValuationOptions()));

    [Theory]
    // Nothing is claimed about traction before Seed, so there is nothing to contradict.
    [InlineData(StartupStage.Idea, 0, 0, "not_applicable")]
    [InlineData(StartupStage.PreSeed, 0, 0, "not_applicable")]
    [InlineData(StartupStage.Mvp, 0, 0, "not_applicable")]
    // Seed claims some traction: users are enough, revenue is enough, nothing is not.
    [InlineData(StartupStage.Seed, 0, 0, "unsupported")]
    [InlineData(StartupStage.Seed, 0, 900, "supported")]
    [InlineData(StartupStage.Seed, 500_000, 0, "supported")]
    // Series A claims revenue — users alone do not bear it out.
    [InlineData(StartupStage.SeriesA, 0, 40_000, "unsupported")]
    [InlineData(StartupStage.SeriesA, 500_000, 40_000, "supported")]
    public void StageConsistency_ShipsInTheProductFactorInputs(
        StartupStage stage, int mrr, int mau, string expected)
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(stage, TractionSignals.From(mrr, mau, 0m)), ValuationBenchmarkSet.Empty, CalculatedAt);

        ScoreInput consistency = result.Factors.Single(f => f.Factor == "Product").Detail.Inputs
            .Single(i => i.Code == "product.input.stage_consistency");

        consistency.Value.Kind.ShouldBe(ScoreValueKind.Code);
        consistency.Value.Code.ShouldBe($"stage_consistency.{expected}");
    }

    /// <summary>
    /// The whole point of the "unsupported" state: an empty metrics tab has to land in it too. If
    /// silence read as "nothing to check", not filling the tab in would be the cheapest way to keep a
    /// declared stage unchallenged — which is the shape of gaming this exists to close.
    /// </summary>
    [Fact]
    public void WithholdingTheMetrics_LandsInTheSameStateAsFallingShort()
    {
        ScoreResult nothingReported = _scoringEngine.Compute(
            Inputs(StartupStage.SeriesA, TractionSignals.Empty), ValuationBenchmarkSet.Empty, CalculatedAt);
        ScoreResult reportedZero = _scoringEngine.Compute(
            Inputs(StartupStage.SeriesA, TractionSignals.From(0m, 0m, 0m)),
            ValuationBenchmarkSet.Empty, CalculatedAt);

        foreach (ScoreResult result in new[] { nothingReported, reportedZero })
        {
            result.Factors.Single(f => f.Factor == "Product").Detail.Inputs
                .Single(i => i.Code == "product.input.stage_consistency")
                .Value.Code.ShouldBe("stage_consistency.unsupported");
        }
    }

    [Theory]
    [InlineData(StartupStage.Idea, 0, false)]
    [InlineData(StartupStage.Mvp, 0, false)]
    [InlineData(StartupStage.Seed, 0, false)]
    [InlineData(StartupStage.Seed, 500_000, true)]
    [InlineData(StartupStage.SeriesA, 0, false)]
    [InlineData(StartupStage.SeriesA, 500_000, true)]
    public void CrossCheckedFlag_IsSetOnlyWhenTheDeclarationHeldUp(
        StartupStage stage, int mrr, bool expected)
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(stage, TractionSignals.From(mrr, 0m, 0m)), ValuationBenchmarkSet.Empty, CalculatedAt);

        result.Factors.Single(f => f.Factor == "Product").Source
            .HasFlag(ScoreFactorSource.CrossChecked).ShouldBe(expected);
    }

    /// <summary>
    /// The cross-check moves no number. Comparing "Series A with revenue" against "Seed with the same
    /// revenue" would compare two different startups, so the pair here differs only in the thing the
    /// check reads: the same declared stage, once with the metrics that bear it out and once with the
    /// same metrics reported one rung lower. Instead the guard is stated directly — the product
    /// factor's components are a function of stage, patents, positioning and roadmap alone.
    /// </summary>
    [Theory]
    [InlineData(StartupStage.Seed)]
    [InlineData(StartupStage.SeriesA)]
    public void CrossCheck_ShouldNotAppearAmongTheProductComponentsOrHints(StartupStage stage)
    {
        ScoreResult borneOut = _scoringEngine.Compute(
            Inputs(stage, TractionSignals.From(500_000m, 900m, 0m)), ValuationBenchmarkSet.Empty, CalculatedAt);
        ScoreResult notBorneOut = _scoringEngine.Compute(
            Inputs(stage, TractionSignals.Empty), ValuationBenchmarkSet.Empty, CalculatedAt);

        ScoreFactorDetail with = borneOut.Factors.Single(f => f.Factor == "Product").Detail;
        ScoreFactorDetail without = notBorneOut.Factors.Single(f => f.Factor == "Product").Detail;

        with.Components.Select(c => (c.Code, c.Points))
            .ShouldBe(without.Components.Select(c => (c.Code, c.Points)));
        with.Hints.Select(h => (h.Code, h.Points))
            .ShouldBe(without.Hints.Select(h => (h.Code, h.Points)));

        // The product sub-score is identical; the traction factor legitimately differs, which is what
        // the metrics are actually scored by.
        borneOut.ProductScore.ShouldBe(notBorneOut.ProductScore);
    }

    /// <summary>
    /// And it moves nothing in the valuation either — the same claim SC-65 makes for the register
    /// check. Both inputs here carry identical scores by construction, so any difference could only
    /// come from the provenance flag.
    /// </summary>
    [Fact]
    public void CrossCheck_ShouldNotMoveTheValuation()
    {
        ScoringInputs inputs = Inputs(StartupStage.Seed, TractionSignals.From(500_000m, 0m, 0m));
        ScoreResult score = _scoringEngine.Compute(inputs, ValuationBenchmarkSet.Empty, CalculatedAt);

        ScoreResult withFlag = score with
        {
            Factors = [.. score.Factors.Select(f => f.Factor == "Product"
                ? f with { Source = f.Source | ScoreFactorSource.CrossChecked }
                : f)]
        };
        ScoreResult withoutFlag = score with
        {
            Factors = [.. score.Factors.Select(f => f.Factor == "Product"
                ? f with { Source = f.Source & ~ScoreFactorSource.CrossChecked }
                : f)]
        };

        ValuationResult with = _valuationCalculator.Compute(withFlag, inputs, ValuationBenchmarkSet.Empty);
        ValuationResult without = _valuationCalculator.Compute(withoutFlag, inputs, ValuationBenchmarkSet.Empty);

        with.Low.ShouldBe(without.Low);
        with.High.ShouldBe(without.High);
        with.Point.ShouldBe(without.Point);
        with.MethodsUsed.ShouldBe(without.MethodsUsed);
    }

    private static ScoringInputs Inputs(StartupStage stage, TractionSignals traction) =>
        new(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            stage,
            Tam: 2_000_000_000m,
            Sam: 500_000_000m,
            Som: 50_000_000m,
            MarketGrowthRate: 12m,
            HasPatents: false,
            Competitors: new CompetitorSignals(TotalCount: 2, WellDocumentedCount: 2),
            Members: [new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 5, false, 0)],
            Traction: traction,
            Product: new ProductSignals(HasArticulatedPositioning: true),
            Roadmap: new RoadmapSignals(ItemCount: 4, DoneCount: 1),
            Partnerships: PartnershipSignals.None,
            Industry: Industry.Other,
            TargetRoundAmount: 40_000_000m);
}
