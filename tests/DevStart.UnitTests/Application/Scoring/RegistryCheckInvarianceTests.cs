using DevStart.Application;
using DevStart.Application.Scoring;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.Scoring;

/// <summary>
/// SC-65's core claim as code: checking an IP record against the register changes provenance and
/// nothing else.
///
/// This is not ceremony. The epic deliberately gives verification no numeric effect — the earlier
/// proposal to move the range band was withdrawn because <c>min</c>/<c>max</c> over the methods make
/// the band inert whenever the methods disagree, which is routine at early stages, and actively
/// perverse when they agree (it would lower <c>valuationHigh</c> for a verified startup). Without a
/// test, a numeric effect is exactly the kind of thing that arrives later by accident and silently.
/// </summary>
public sealed class RegistryCheckInvarianceTests
{
    private static readonly DateTime CalculatedAt = new(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc);

    private readonly IScoringEngine _scoringEngine = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IScoringEngine>();

    private readonly IValuationCalculator _valuationCalculator =
        new ValuationCalculator(new OptionsWrapper<ValuationOptions>(new ValuationOptions()));

    public static TheoryData<string, StartupStage, bool> Cases() => new()
    {
        { "idea, no patents declared", StartupStage.Idea, false },
        { "idea, patents declared", StartupStage.Idea, true },
        { "mvp, patents declared", StartupStage.Mvp, true },
        { "seed, patents declared", StartupStage.Seed, true },
        { "series a, no patents declared", StartupStage.SeriesA, false },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void RegistryCheck_ShouldNotMoveTheScore(string name, StartupStage stage, bool hasPatents)
    {
        ScoreResult without = _scoringEngine.Compute(
            Inputs(stage, hasPatents, registryChecked: false), Benchmarks(), CalculatedAt);
        ScoreResult with = _scoringEngine.Compute(
            Inputs(stage, hasPatents, registryChecked: true), Benchmarks(), CalculatedAt);

        with.TotalScore.ShouldBe(without.TotalScore, name);
        with.ProductScore.ShouldBe(without.ProductScore, name);
        with.TeamScore.ShouldBe(without.TeamScore, name);
        with.MarketScore.ShouldBe(without.MarketScore, name);
        with.TractionScore.ShouldBe(without.TractionScore, name);
        with.CompetitionScore.ShouldBe(without.CompetitionScore, name);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void RegistryCheck_ShouldNotMoveTheValuationRangeOrTheMethods(
        string name, StartupStage stage, bool hasPatents)
    {
        ScoringInputs withoutInputs = Inputs(stage, hasPatents, registryChecked: false);
        ScoringInputs withInputs = Inputs(stage, hasPatents, registryChecked: true);

        ValuationResult without = _valuationCalculator.Compute(
            _scoringEngine.Compute(withoutInputs, Benchmarks(), CalculatedAt), withoutInputs, Benchmarks());
        ValuationResult with = _valuationCalculator.Compute(
            _scoringEngine.Compute(withInputs, Benchmarks(), CalculatedAt), withInputs, Benchmarks());

        with.Low.ShouldBe(without.Low, name);
        with.High.ShouldBe(without.High, name);
        with.Point.ShouldBe(without.Point, name);
        with.MethodsUsed.ShouldBe(without.MethodsUsed, name);
    }

    [Fact]
    public void RegistryCheck_ShouldOnlyAddTheProvenanceFlagOnTheProductFactor()
    {
        ScoreResult without = _scoringEngine.Compute(
            Inputs(StartupStage.Mvp, hasPatents: true, registryChecked: false), Benchmarks(), CalculatedAt);
        ScoreResult with = _scoringEngine.Compute(
            Inputs(StartupStage.Mvp, hasPatents: true, registryChecked: true), Benchmarks(), CalculatedAt);

        ScoreFactorBreakdown productWithout = without.Factors.Single(f => f.Factor == "Product");
        ScoreFactorBreakdown productWith = with.Factors.Single(f => f.Factor == "Product");

        productWithout.Source.HasFlag(ScoreFactorSource.RegistryChecked).ShouldBeFalse();
        productWith.Source.HasFlag(ScoreFactorSource.RegistryChecked).ShouldBeTrue();

        // Exactly one bit differs, and it is the new one: the self-reported and platform-derived
        // provenance of the factor is untouched.
        (productWith.Source ^ productWithout.Source).ShouldBe(ScoreFactorSource.RegistryChecked);

        // Every other factor's provenance is untouched too — the flag is about IP records, and nothing
        // else in the engine reads it.
        foreach (ScoreFactorBreakdown factor in with.Factors.Where(f => f.Factor != "Product"))
        {
            factor.Source.ShouldBe(without.Factors.Single(f => f.Factor == factor.Factor).Source);
        }
    }

    [Fact]
    public void RegistryCheck_ShouldNotChangeTheProductComponentsOrHints()
    {
        ScoreResult without = _scoringEngine.Compute(
            Inputs(StartupStage.Idea, hasPatents: true, registryChecked: false), Benchmarks(), CalculatedAt);
        ScoreResult with = _scoringEngine.Compute(
            Inputs(StartupStage.Idea, hasPatents: true, registryChecked: true), Benchmarks(), CalculatedAt);

        ScoreFactorDetail detailWithout = without.Factors.Single(f => f.Factor == "Product").Detail;
        ScoreFactorDetail detailWith = with.Factors.Single(f => f.Factor == "Product").Detail;

        detailWith.Components.Select(c => (c.Code, c.Points))
            .ShouldBe(detailWithout.Components.Select(c => (c.Code, c.Points)));

        // No hint appears either: the invitation to enter numbers lives on the Product tab, not in the
        // scoring detail, which is about unmet conditions worth points. A hint worth zero points would
        // break that meaning — and would put the guard test one careless commit away from failing.
        detailWith.Hints.Select(h => h.Code).ShouldBe(detailWithout.Hints.Select(h => h.Code));
    }

    private static ValuationBenchmarkSet Benchmarks() => new(
        new Dictionary<(Industry, StartupStage), decimal>
        {
            [(Industry.Other, StartupStage.Idea)] = 60_000_000m,
            [(Industry.Other, StartupStage.PreSeed)] = 120_000_000m,
            [(Industry.Other, StartupStage.Mvp)] = 250_000_000m,
            [(Industry.Other, StartupStage.Seed)] = 400_000_000m,
        },
        new Dictionary<Industry, decimal>());

    private static ScoringInputs Inputs(StartupStage stage, bool hasPatents, bool registryChecked) =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            stage,
            Tam: 5_000_000_000m,
            Sam: 1_000_000_000m,
            Som: 100_000_000m,
            MarketGrowthRate: 25m,
            HasPatents: hasPatents,
            Competitors: new CompetitorSignals(TotalCount: 3, WellDocumentedCount: 3),
            Members:
            [
                new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 6, true, 1),
                new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CTO, 4, false, 0),
            ],
            Traction: TractionSignals.From(1_500_000m, 20_000m, 15m),
            Product: new ProductSignals(HasArticulatedPositioning: true),
            Roadmap: new RoadmapSignals(ItemCount: 5, DoneCount: 2),
            Partnerships: new PartnershipSignals(TotalCount: 2, WorkedOutCount: 2),
            Industry: Industry.Other,
            TargetRoundAmount: 30_000_000m,
            HasRegistryCheckedIp: registryChecked);
}
