using DevStart.Application;
using DevStart.Application.Scoring;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.UnitTests.Application.Scoring;

/// <summary>
/// The per-factor detail: components, raw inputs and improvement hints. The suite is built around a
/// matrix of profiles chosen to reach every branch of every factor — the golden-code test below fails
/// if the matrix stops covering a branch, so the coverage cannot rot silently.
/// </summary>
public sealed class ScoringEngineDetailTests
{
    private static readonly DateTime CalculatedAt = new(2026, 5, 16, 10, 0, 0, DateTimeKind.Utc);

    private readonly IScoringEngine _scoringEngine = new ServiceCollection()
        .AddApplication()
        .BuildServiceProvider()
        .GetRequiredService<IScoringEngine>();

    // ---- The invariant the whole design rests on ----------------------------------------------

    [Fact]
    public void Components_ShouldSumToTheFactorScore_ForEveryFactor()
    {
        foreach ((string name, ScoringInputs inputs, ValuationBenchmarkSet benchmarks) in Matrix())
        {
            ScoreResult result = _scoringEngine.Compute(inputs, benchmarks, CalculatedAt);

            foreach (ScoreFactorBreakdown factor in result.Factors.Where(f => f.Score.HasValue))
            {
                factor.Detail.Components.Sum(c => c.Points)
                    .ShouldBe(factor.Score!.Value, $"{name} / {factor.Factor}");
            }
        }
    }

    [Fact]
    public void DroppedOutFactor_ShouldCarryNoComponents()
    {
        foreach ((string name, ScoringInputs inputs, ValuationBenchmarkSet benchmarks) in Matrix())
        {
            ScoreResult result = _scoringEngine.Compute(inputs, benchmarks, CalculatedAt);

            foreach (ScoreFactorBreakdown factor in result.Factors.Where(f => !f.Score.HasValue))
            {
                factor.Detail.Components.ShouldBeEmpty($"{name} / {factor.Factor}");
            }
        }
    }

    [Theory]
    // Team: SerialWithExit 90 + full C-suite 15 = 105.
    [InlineData("Team", "team.clamp", -5)]
    // Market: TAM ≥ ₽10B 90 + CAGR ≥ 20% 25 + funnel 5 = 120.
    [InlineData("Market", "market.clamp", -20)]
    // Product: SeriesA 85 + patents 10 + positioning 5 + roadmap 5 = 105.
    [InlineData("Product", "product.clamp", -5)]
    public void ClampedFactor_ShouldCarryANegativeClampComponentLast(
        string factorName, string clampCode, int clampPoints)
    {
        ScoreResult result = _scoringEngine.Compute(Maxed(), ValuationBenchmarkSet.Empty, CalculatedAt);

        ScoreFactorBreakdown factor = result.Factors.Single(f => f.Factor == factorName);

        factor.Score.ShouldBe(100m);
        factor.Detail.Components[^1].Code.ShouldBe(clampCode);
        factor.Detail.Components[^1].Points.ShouldBe(clampPoints);
        factor.Detail.Components.Sum(c => c.Points).ShouldBe(100m);
    }

    [Fact]
    public void CompetitionAtAnEmptySector_ShouldClampToTheTopOfTheScale()
    {
        // Intensity 0 → base 100, plus the saturated documentation bonus 30 = 130.
        ScoreResult result = _scoringEngine.Compute(
            Maxed(competitors: new CompetitorSignals(3, 3), industry: Industry.Saas),
            Benchmarks(Industry.Saas, intensity: 0m),
            CalculatedAt);

        ScoreFactorBreakdown competition = result.Factors.Single(f => f.Factor == "Competition");

        competition.Score.ShouldBe(100m);
        competition.Detail.Components[^1].ShouldBe(new ScoreComponent("competition.clamp", -30m));
        competition.Detail.Components.Sum(c => c.Points).ShouldBe(100m);
    }

    // ---- Hints ---------------------------------------------------------------------------------

    [Fact]
    public void Hints_ShouldBePositive_AndFitTheFactorHeadroom()
    {
        foreach ((string name, ScoringInputs inputs, ValuationBenchmarkSet benchmarks) in Matrix())
        {
            ScoreResult result = _scoringEngine.Compute(inputs, benchmarks, CalculatedAt);

            foreach (ScoreFactorBreakdown factor in result.Factors)
            {
                foreach (ScoreHint hint in factor.Detail.Hints)
                {
                    string because = $"{name} / {factor.Factor} / {hint.Code}";

                    hint.Points.ShouldBeGreaterThan(0m, because);

                    // A participating factor can only gain what is left of the scale. A dropped-out one
                    // has no score to add to — its hint states the score the factor would have instead.
                    decimal ceiling = factor.Score.HasValue ? 100m - factor.Score.Value : 100m;
                    hint.Points.ShouldBeLessThanOrEqualTo(ceiling, because);

                    hint.EnablesFactor.ShouldBe(!factor.Score.HasValue, because);
                }
            }
        }
    }

    /// <summary>
    /// The methodology's opening promise is that no action in the UI raises the score by hiding
    /// information — extended here to overstating it. A hint is the platform actively asking for an
    /// input, so it must never point at one the founder can simply inflate, nor at anything that is
    /// satisfied by deleting data. This is the policy of docs/scoring-methodology.md as code.
    /// </summary>
    [Fact]
    public void Hints_ShouldNeverPromoteGameableSelfDeclaration()
    {
        string[] forbidden =
        [
            "patents",      // unverified one-click boolean, pending a Rospatent check
            "stage",        // raising it moves the weights too — that is a misdeclaration, not progress
            "cagr",         // no honest fill hint exists: the lowest CAGR tier is worth 0
            "tam_",         // the *tier* jump; "fill_tam" (worth the floor tier) is allowed
            "experience",   // founder experience is only un-gameable because we never prompt for it
            "serial",
            "exit",
            "total_cards",  // the number of competitor cards is exactly the driver v5 removed
            "intensity",    // the startup cannot edit the sector benchmark
            "delete",
            "remove",
        ];

        foreach ((string name, ScoringInputs inputs, ValuationBenchmarkSet benchmarks) in Matrix())
        {
            ScoreResult result = _scoringEngine.Compute(inputs, benchmarks, CalculatedAt);

            foreach (string code in result.Factors.SelectMany(f => f.Detail.Hints).Select(h => h.Code))
            {
                foreach (string fragment in forbidden)
                {
                    code.ShouldNotContain(fragment, customMessage: $"{name}: hint '{code}' is gameable");
                }
            }
        }
    }

    [Theory]
    // Nothing on file → the first users are worth the users-only rung.
    [InlineData(0, 0, 0, false, "traction.tier.no_data", "traction.hint.first_users", 35)]
    // Users but no revenue: charging lands on the flat rung (50) or, at MoM ≥ 10%, the growth one (70).
    [InlineData(0, 1_000, 0, true, "traction.tier.users_only", "traction.hint.first_revenue", 15)]
    [InlineData(0, 1_000, 12, true, "traction.tier.users_only", "traction.hint.first_revenue", 35)]
    // Shrinking revenue → stopping the decline is worth the flat rung.
    [InlineData(500_000, 0, -5, true, "traction.tier.declining", "traction.hint.stop_decline", 25)]
    [InlineData(500_000, 0, 5, true, "traction.tier.flat", "traction.hint.growth_10", 20)]
    [InlineData(500_000, 0, 12, true, "traction.tier.early_growth", "traction.hint.mrr_1m", 10)]
    [InlineData(2_000_000, 0, 15, true, "traction.tier.growing", "traction.hint.mrr_4m", 15)]
    public void TractionHint_ShouldTargetTheNextRungOnly(
        int mrr, int mau, int mom, bool hasData, string tierCode, string hintCode, int points)
    {
        ScoreFactorDetail detail = TractionDetail(new TractionSignals(mrr, mau, mom, HasData: hasData));

        detail.Components.Single().Code.ShouldBe(tierCode);
        detail.Hints.Single().Code.ShouldBe(hintCode);
        detail.Hints.Single().Points.ShouldBe(points);
    }

    [Fact]
    public void TractionHint_ShouldBeSuppressed_WhenChargingWouldLowerTheScore()
    {
        // The single MoM metric selects the revenue rungs whatever it measured pre-revenue, so at a
        // negative reading "start charging" lands on declining (25), below the current users-only 35.
        ScoreFactorDetail detail = TractionDetail(new TractionSignals(0m, 1_000m, -5m, HasData: true));

        detail.Components.Single().Code.ShouldBe("traction.tier.users_only");
        detail.Hints.ShouldBeEmpty();
    }

    [Fact]
    public void TractionHint_ShouldBeAbsent_AtTheTopRung()
    {
        ScoreFactorDetail detail = TractionDetail(new TractionSignals(4_000_000m, 0m, 20m, HasData: true));

        detail.Components.Single().ShouldBe(new ScoreComponent("traction.tier.scaling", 95m));
        detail.Hints.ShouldBeEmpty();
    }

    [Fact]
    public void TeamHint_ShouldNameTheMissingPositions_AndFitTheHeadroom()
    {
        // SerialWithExit base 90 with no C-suite coverage: the bonus is worth 15, but only 10 fit.
        ScoreResult result = _scoringEngine.Compute(
            Inputs(
                StartupStage.Seed,
                members: [new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.Other, 8, true, 2)]),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        ScoreFactorBreakdown team = result.Factors.Single(f => f.Factor == "Team");
        ScoreHint hint = team.Detail.Hints.Single();

        team.Score.ShouldBe(90m);
        hint.Code.ShouldBe("team.hint.csuite");
        hint.Points.ShouldBe(10m);
        hint.Targets.Select(t => t.Code)
            .ShouldBe(["position.ceo", "position.cto", "position.cmo"]);
    }

    [Fact]
    public void AbsentCompetition_ShouldStillCarryInputsAndAnEnablingHint()
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(StartupStage.Idea, competitors: CompetitorSignals.None),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        ScoreFactorBreakdown competition = result.Factors.Single(f => f.Factor == "Competition");

        competition.Score.ShouldBeNull();
        competition.Detail.Components.ShouldBeEmpty();

        // The reader still sees *what* is missing.
        competition.Detail.Inputs.Select(i => i.Code)
            .ShouldBe(["competition.input.total_cards", "competition.input.documented_cards", "competition.input.sector_intensity"]);
        competition.Detail.Inputs.Single(i => i.Code == "competition.input.sector_intensity")
            .Value.Kind.ShouldBe(ScoreValueKind.None);

        ScoreHint hint = competition.Detail.Hints.Single();
        hint.Code.ShouldBe("competition.hint.first_documented_card");
        hint.EnablesFactor.ShouldBeTrue();
        // Neutral base 50 plus the first documented card's +10 — a score, not a delta.
        hint.Points.ShouldBe(60m);
    }

    [Fact]
    public void CompetitionHint_ShouldSaturateWithTheBonus()
    {
        ScoreFactorDetail twoDocumented = _scoringEngine
            .Compute(Inputs(StartupStage.Idea, competitors: new CompetitorSignals(4, 2)),
                ValuationBenchmarkSet.Empty, CalculatedAt)
            .Factors.Single(f => f.Factor == "Competition").Detail;

        ScoreHint hint = twoDocumented.Hints.Single();
        hint.Code.ShouldBe("competition.hint.document_card");
        hint.Points.ShouldBe(10m);
        hint.Targets.Single().Number.ShouldBe(3m);

        ScoreFactorDetail saturated = _scoringEngine
            .Compute(Inputs(StartupStage.Idea, competitors: new CompetitorSignals(9, 3)),
                ValuationBenchmarkSet.Empty, CalculatedAt)
            .Factors.Single(f => f.Factor == "Competition").Detail;

        saturated.Hints.ShouldBeEmpty();
    }

    // ---- Inputs --------------------------------------------------------------------------------

    [Fact]
    public void Inputs_ShouldReportAbsentValues_RatherThanOmittingTheRow()
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(StartupStage.Idea), ValuationBenchmarkSet.Empty, CalculatedAt);

        ScoreInput tam = result.Factors.Single(f => f.Factor == "Market").Detail.Inputs
            .Single(i => i.Code == "market.input.tam");

        tam.Value.Kind.ShouldBe(ScoreValueKind.None);
        tam.Value.Number.ShouldBeNull();
    }

    [Fact]
    public void Inputs_ShouldCarryRawUnformattedValues()
    {
        ScoreResult result = _scoringEngine.Compute(
            Inputs(
                StartupStage.Mvp,
                tam: 2_500_000_000m,
                cagr: 14m,
                traction: new TractionSignals(1_200_000m, 4_300m, 11m, MrrIsProxy: true, HasData: true)),
            ValuationBenchmarkSet.Empty,
            CalculatedAt);

        IReadOnlyList<ScoreInput> market = result.Factors.Single(f => f.Factor == "Market").Detail.Inputs;
        market.Single(i => i.Code == "market.input.tam").Value
            .ShouldBe(new ScoreValue(ScoreValueKind.Money, 2_500_000_000m));
        market.Single(i => i.Code == "market.input.cagr").Value
            .ShouldBe(new ScoreValue(ScoreValueKind.Percent, 14m));

        IReadOnlyList<ScoreInput> traction = result.Factors.Single(f => f.Factor == "Traction").Detail.Inputs;
        traction.Single(i => i.Code == "traction.input.mau").Value
            .ShouldBe(new ScoreValue(ScoreValueKind.Count, 4_300m));
        traction.Single(i => i.Code == "traction.input.mrr_is_proxy").Value
            .ShouldBe(new ScoreValue(ScoreValueKind.Flag, 1m));

        ScoreInput stage = result.Factors.Single(f => f.Factor == "Product").Detail.Inputs
            .Single(i => i.Code == "product.input.stage");
        stage.Value.Kind.ShouldBe(ScoreValueKind.Code);
        stage.Value.Code.ShouldBe("stage.mvp");
    }

    // ---- Code stability -------------------------------------------------------------------------

    /// <summary>
    /// Codes are the contract: the client keys its Russian labels off them, so a rename is a silent
    /// break. The policy is append-only within a methodology version — a rule whose *meaning* changes
    /// gets a new code, and codes are never renamed. This test pins the whole emitted vocabulary, so
    /// both a rename and a dropped branch fail loudly.
    /// </summary>
    [Fact]
    public void EmittedCodes_ShouldNotChangeSilently()
    {
        var emitted = new HashSet<string>(StringComparer.Ordinal);

        foreach ((_, ScoringInputs inputs, ValuationBenchmarkSet benchmarks) in Matrix())
        {
            ScoreResult result = _scoringEngine.Compute(inputs, benchmarks, CalculatedAt);

            foreach (ScoreFactorDetail detail in result.Factors.Select(f => f.Detail))
            {
                emitted.UnionWith(detail.Components.Select(c => c.Code));
                emitted.UnionWith(detail.Inputs.Select(i => i.Code));
                emitted.UnionWith(detail.Hints.Select(h => h.Code));
                emitted.UnionWith(detail.Inputs.Select(i => i.Value.Code).OfType<string>());
                emitted.UnionWith(detail.Hints.SelectMany(h => h.Targets).Select(t => t.Code).OfType<string>());
            }
        }

        // The set is the contract, not its order — compare unordered so the golden list can stay
        // grouped by factor for a human reader.
        emitted.ShouldBe(
        [
            "competition.base.benchmark",
            "competition.base.neutral",
            "competition.bonus.documented",
            "competition.clamp",
            "competition.hint.document_card",
            "competition.hint.first_documented_card",
            "competition.input.documented_cards",
            "competition.input.sector_intensity",
            "competition.input.total_cards",
            "founder_tier.industry_experience",
            "founder_tier.no_experience",
            "founder_tier.serial",
            "founder_tier.serial_with_exit",
            "market.base.no_tam",
            "market.base.tam_10b_plus",
            "market.base.tam_1_10b",
            "market.base.tam_sub_1b",
            "market.bonus.cagr_10_20",
            "market.bonus.cagr_20_plus",
            "market.bonus.funnel",
            "market.clamp",
            "market.hint.fill_tam",
            "market.hint.funnel",
            "market.input.cagr",
            "market.input.sam",
            "market.input.som",
            "market.input.tam",
            "pool.all_members",
            "pool.founders",
            "position.ceo",
            "position.cmo",
            "position.cto",
            "product.base.stage_idea",
            "product.base.stage_mvp",
            "product.base.stage_pre_seed",
            "product.base.stage_seed",
            "product.base.stage_series_a",
            "product.bonus.patents",
            "product.bonus.positioning",
            "product.bonus.roadmap",
            "product.clamp",
            "product.hint.positioning",
            "product.hint.roadmap",
            "product.input.has_patents",
            "product.input.has_positioning",
            "product.input.roadmap_items",
            "product.input.stage",
            "stage.idea",
            "stage.mvp",
            "stage.pre_seed",
            "stage.seed",
            "stage.series_a",
            "team.base.industry_experience",
            "team.base.no_experience",
            "team.base.no_members",
            "team.base.serial",
            "team.base.serial_with_exit",
            "team.bonus.csuite",
            "team.clamp",
            "team.hint.add_members",
            "team.hint.csuite",
            "team.input.experience_pool",
            "team.input.founder_tier",
            "team.input.has_ceo",
            "team.input.has_cmo",
            "team.input.has_cto",
            "team.input.member_count",
            "traction.hint.first_revenue",
            "traction.hint.first_users",
            "traction.hint.growth_10",
            "traction.hint.mrr_1m",
            "traction.hint.mrr_4m",
            "traction.hint.stop_decline",
            "traction.input.mau",
            "traction.input.mom_growth",
            "traction.input.mrr",
            "traction.input.mrr_is_proxy",
            "traction.tier.declining",
            "traction.tier.early_growth",
            "traction.tier.flat",
            "traction.tier.growing",
            "traction.tier.no_data",
            "traction.tier.scaling",
            "traction.tier.users_only",
        ], ignoreOrder: true);
    }

    // ---- The matrix ----------------------------------------------------------------------------

    /// <summary>
    /// Profiles chosen to reach every branch of every factor. Kept honest by
    /// <see cref="EmittedCodes_ShouldNotChangeSilently"/>, which fails as soon as a branch stops
    /// being exercised.
    /// </summary>
    private static IEnumerable<(string Name, ScoringInputs Inputs, ValuationBenchmarkSet Benchmarks)> Matrix()
    {
        yield return ("empty", Inputs(StartupStage.Idea), ValuationBenchmarkSet.Empty);

        yield return (
            "no-experience founder, sub-₽1B market, undocumented cards",
            Inputs(
                StartupStage.PreSeed,
                tam: 500_000_000m,
                cagr: 4m,
                competitors: new CompetitorSignals(4, 0),
                members: [new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 1, false, 0)],
                traction: new TractionSignals(0m, 1_200m, 0m, HasData: true)),
            ValuationBenchmarkSet.Empty);

        yield return (
            "industry founder, mid market with CAGR bump, declining revenue",
            Inputs(
                StartupStage.Mvp,
                tam: 3_000_000_000m,
                sam: 900_000_000m,
                som: 100_000_000m,
                cagr: 15m,
                competitors: new CompetitorSignals(2, 1),
                members: [new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CTO, 6, false, 0)],
                traction: new TractionSignals(800_000m, 0m, -3m, HasData: true),
                product: new ProductSignals(HasArticulatedPositioning: true)),
            ValuationBenchmarkSet.Empty);

        yield return (
            "serial founder without exit, flat revenue, no founder flag",
            Inputs(
                StartupStage.Seed,
                tam: 1_000_000_000m,
                cagr: 25m,
                hasPatents: true,
                competitors: new CompetitorSignals(3, 2),
                // Nobody is flagged Founder → the experience pool falls back to all members.
                members: [new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.COO, 2, false, 3)],
                traction: new TractionSignals(600_000m, 0m, 4m, HasData: true),
                roadmap: new RoadmapSignals(ItemCount: 5, DoneCount: 1)),
            ValuationBenchmarkSet.Empty);

        yield return (
            "early growth, sector benchmark present",
            Inputs(
                StartupStage.Seed,
                tam: 2_000_000_000m,
                cagr: 12m,
                competitors: new CompetitorSignals(1, 1),
                members: [new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 4, false, 0)],
                traction: new TractionSignals(400_000m, 900m, 14m, HasData: true),
                industry: Industry.Saas),
            Benchmarks(Industry.Saas, intensity: 70m));

        yield return (
            "growing revenue below the top rung",
            Inputs(
                StartupStage.SeriesA,
                tam: 9_000_000_000m,
                cagr: 11m,
                competitors: new CompetitorSignals(2, 2),
                members:
                [
                    new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 3, false, 0),
                    new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CTO, 2, false, 0),
                ],
                traction: new TractionSignals(2_000_000m, 0m, 15m, HasData: true)),
            ValuationBenchmarkSet.Empty);

        yield return ("maxed out", Maxed(), ValuationBenchmarkSet.Empty);

        yield return (
            "maxed out in an empty sector",
            Maxed(competitors: new CompetitorSignals(3, 3), industry: Industry.Saas),
            Benchmarks(Industry.Saas, intensity: 0m));
    }

    // Every factor pushed past the top of the scale, so all three clamp components appear.
    private static ScoringInputs Maxed(CompetitorSignals? competitors = null, Industry industry = Industry.Other) =>
        Inputs(
            StartupStage.SeriesA,
            tam: 10_000_000_000m,
            sam: 4_000_000_000m,
            som: 900_000_000m,
            cagr: 22m,
            hasPatents: true,
            competitors: competitors ?? new CompetitorSignals(3, 3),
            members:
            [
                new MemberInput(Guid.NewGuid(), StartupRole.Founder, StartupPosition.CEO, 9, true, 2),
                new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CTO, 5, false, 0),
                new MemberInput(Guid.NewGuid(), StartupRole.Member, StartupPosition.CMO, 5, false, 0),
            ],
            traction: new TractionSignals(5_000_000m, 12_000m, 25m, HasData: true),
            product: new ProductSignals(HasArticulatedPositioning: true),
            roadmap: new RoadmapSignals(ItemCount: 6, DoneCount: 3),
            industry: industry);

    // ---- Helpers -------------------------------------------------------------------------------

    private ScoreFactorDetail TractionDetail(TractionSignals traction) =>
        _scoringEngine
            .Compute(Inputs(StartupStage.Seed, traction: traction), ValuationBenchmarkSet.Empty, CalculatedAt)
            .Factors.Single(f => f.Factor == "Traction").Detail;

    private static ValuationBenchmarkSet Benchmarks(Industry industry, decimal intensity) =>
        ValuationBenchmarkSet.FromRows(
            [new ValuationBenchmarkRow(
                BenchmarkMetricType.CompetitionIntensity, industry, null, intensity,
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))],
            CalculatedAt);

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
