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

        result.TotalScore.ShouldBe(5.75m);
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
        result.TotalScore.ShouldBe(80.75m);
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
        result.TotalScore.ShouldBe(75.25m);
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
}
