using DevStart.Application;
using DevStart.Application.Scoring;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.Startups;
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
            new ScoringInputs(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                StartupStage.Idea,
                Tam: null,
                MarketGrowthRate: null,
                HasPatents: false,
                CompetitorsCount: 4,
                Members: [],
                LatestMetrics: new Dictionary<MetricType, decimal>()),
            CalculatedAt);

        // Idea-stage weights: product 0.20, competition 0.10 → 15*0.20 + 35*0.10 = 6.5
        result.TotalScore.ShouldBe(6.5m);
        result.TeamScore.ShouldBe(0m);
        result.MarketScore.ShouldBe(0m);
        result.ProductScore.ShouldBe(15m);
        result.TractionScore.ShouldBe(0m);
        result.CompetitionScore.ShouldBe(35m);
        result.ValuationLow.ShouldBe(0m);
        result.ValuationHigh.ShouldBe(0m);
        result.MethodsUsed.ShouldBeEmpty();
        result.CalculatedAt.ShouldBe(CalculatedAt);
    }

    [Fact]
    public void Compute_ShouldApplyTeamMarketProductTractionAndCompetitionRules()
    {
        ScoreResult result = _scoringEngine.Compute(
            new ScoringInputs(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                StartupStage.Mvp,
                Tam: 10_000_000_000m,
                MarketGrowthRate: 20m,
                HasPatents: true,
                CompetitorsCount: 0,
                Members:
                [
                    new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 4, false, 0),
                    new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CTO, 1, false, 0),
                    new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CMO, 1, false, 0)
                ],
                LatestMetrics: new Dictionary<MetricType, decimal>
                {
                    [MetricType.Mrr] = 4_000_000m,
                    [MetricType.MomGrowth] = 20m
                }),
            CalculatedAt);

        result.TeamScore.ShouldBe(75m);
        result.MarketScore.ShouldBe(100m);
        result.ProductScore.ShouldBe(70m);
        result.TractionScore.ShouldBe(95m);
        result.CompetitionScore.ShouldBe(85m);
        // Mvp-stage weights: 75*.25 + 100*.25 + 70*.15 + 95*.25 + 85*.10 = 86.5
        result.TotalScore.ShouldBe(86.5m);
    }

    [Fact]
    public void Compute_ShouldUseHighestFounderTierAndRoundTotal()
    {
        ScoreResult result = _scoringEngine.Compute(
            new ScoringInputs(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                StartupStage.Seed,
                Tam: 1_000_000_000m,
                MarketGrowthRate: 10m,
                HasPatents: false,
                CompetitorsCount: 2,
                Members:
                [
                    new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 1, false, 0),
                    new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CTO, 1, true, 0),
                    new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CMO, null, null, null)
                ],
                LatestMetrics: new Dictionary<MetricType, decimal>
                {
                    [MetricType.Mrr] = 100_000m,
                    [MetricType.MomGrowth] = 10m
                }),
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
            new ScoringInputs(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                StartupStage.PreSeed,
                Tam: 500_000_000m,
                MarketGrowthRate: 5m,
                HasPatents: false,
                CompetitorsCount: 3,
                Members:
                [
                    new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 2, false, 0)
                ],
                LatestMetrics: new Dictionary<MetricType, decimal>
                {
                    [MetricType.Mau] = 1_000m
                }),
            CalculatedAt);

        result.MarketScore.ShouldBe(20m);
        result.TractionScore.ShouldBe(35m);
        result.CompetitionScore.ShouldBe(60m);
    }

    [Fact]
    public void Compute_ShouldScoreDecliningRevenueBelowFlatGrowth()
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(StartupStage.Seed, metrics: new Dictionary<MetricType, decimal>
            {
                [MetricType.Mrr] = 2_000_000m,
                [MetricType.MomGrowth] = -5m
            }),
            CalculatedAt);

        // MRR > 0 but shrinking → decline tier (25), below the flat-growth 50.
        result.TractionScore.ShouldBe(25m);
    }

    [Fact]
    public void Compute_ShouldTreatNegativeMetricsAsZero()
    {
        ScoreResult withMau = _scoringEngine.Compute(
            Inputs(StartupStage.Seed, metrics: new Dictionary<MetricType, decimal>
            {
                [MetricType.Mrr] = -100m,
                [MetricType.Mau] = 500m
            }),
            CalculatedAt);

        // Negative MRR is floored to 0 → falls into the MAU-only branch.
        withMau.TractionScore.ShouldBe(35m);

        ScoreResult noSignal = _scoringEngine.Compute(
            Inputs(StartupStage.Seed, metrics: new Dictionary<MetricType, decimal>
            {
                [MetricType.Mrr] = -100m,
                [MetricType.Mau] = -10m
            }),
            CalculatedAt);

        noSignal.TractionScore.ShouldBe(0m);
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
            CalculatedAt);

        // Falls back to the highest tier among all members (SerialWithExit = 90), not NoExperience (30).
        result.TeamScore.ShouldBe(90m);
    }

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

    private static ScoringInputs Inputs(
        StartupStage stage,
        IReadOnlyList<MemberInput>? members = null,
        IReadOnlyDictionary<MetricType, decimal>? metrics = null) =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            stage,
            Tam: null,
            MarketGrowthRate: null,
            HasPatents: false,
            CompetitorsCount: 0,
            Members: members ?? [],
            LatestMetrics: metrics ?? new Dictionary<MetricType, decimal>());
}
