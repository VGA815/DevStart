using DevStart.Application.Scoring;
using DevStart.Application.StartupPatents;
using DevStart.Domain.StartupCompetitors;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.StartupPartnerships;
using DevStart.Domain.StartupProducts;
using DevStart.Domain.StartupRoadmapItems;
using DevStart.Domain.Startups;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.Scoring;

public sealed class ScoringDataProviderTests
{
    private static readonly DateTime Now = new(2026, 5, 26, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly Guid _startupId = Guid.NewGuid();

    // The real resolver over the same in-memory context: the provider asks it whether any IP record is
    // registry-checked, and with no records and no ИНН the honest answer is "no".
    private IScoringDataProvider CreateSut() => new ScoringDataProvider(_db, new PatentRegistryResolver(_db));

    private void SeedStartup(StartupStage stage = StartupStage.Seed, decimal? tam = null, decimal? sam = null, decimal? som = null)
    {
        _db.Startups.Add(new Startup
        {
            Id = _startupId,
            Name = "Acme",
            PublicEmail = "acme@example.com",
            Stage = stage,
            Tam = tam,
            Sam = sam,
            Som = som,
            HasPatents = true,
            CreatedAt = Now,
            UpdatedAt = Now,
        });
    }

    [Fact]
    public async Task GetInputsAsync_ReturnsNotFound_WhenStartupMissing()
    {
        Result<ScoringInputs> result = await CreateSut().GetInputsAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Startups.NotFound");
    }

    [Fact]
    public async Task GetInputsAsync_AssemblesStartupFieldsMembersAndCompetitorSignals()
    {
        SeedStartup(StartupStage.Mvp, tam: 1_000_000_000m, sam: 500_000_000m, som: 100_000_000m);
        _db.StartupMembers.Add(StartupMember.Create(
            Guid.NewGuid(), _startupId, StartupRole.Founder, isPublic: true, Now,
            StartupPosition.CEO, yearsOfExperience: 5, hasPriorExit: true, previousStartupsCount: 2));
        _db.StartupCompetitors.Add(StartupCompetitor.Create(
            _startupId, "Rival", "https://rival.com", null, "Bigger sales team", null, Now));
        _db.StartupCompetitors.Add(StartupCompetitor.Create(
            _startupId, "Rival2", "https://rival2.com", null, null, null, Now));
        await _db.SaveChangesAsync();

        ScoringInputs inputs = (await CreateSut().GetInputsAsync(_startupId, CancellationToken.None)).Value;

        inputs.Stage.ShouldBe(StartupStage.Mvp);
        inputs.Tam.ShouldBe(1_000_000_000m);
        inputs.Sam.ShouldBe(500_000_000m);
        inputs.Som.ShouldBe(100_000_000m);
        inputs.HasPatents.ShouldBeTrue();
        inputs.Competitors.TotalCount.ShouldBe(2);
        // Only the card carrying an analysis counts — a website alone is not enough.
        inputs.Competitors.WellDocumentedCount.ShouldBe(1);
        inputs.Members.Count.ShouldBe(1);
        inputs.Members[0].HasPriorExit.ShouldBe(true);
    }

    [Theory]
    // website + strengths, website + weaknesses → documented
    [InlineData("https://rival.com", "Bigger sales team", null, 1)]
    [InlineData("https://rival.com", null, "No mobile app", 1)]
    [InlineData("https://rival.com", "Bigger sales team", "No mobile app", 1)]
    // website alone, or an analysis with no website → not documented
    [InlineData("https://rival.com", null, null, 0)]
    [InlineData("https://rival.com", "   ", "  ", 0)]
    public async Task GetInputsAsync_CountsOnlyCardsCarryingAnAnalysis(
        string website, string? strengths, string? weaknesses, int expected)
    {
        SeedStartup();
        _db.StartupCompetitors.Add(StartupCompetitor.Create(
            _startupId, "Rival", website, null, strengths, weaknesses, Now));
        await _db.SaveChangesAsync();

        ScoringInputs inputs = (await CreateSut().GetInputsAsync(_startupId, CancellationToken.None)).Value;

        inputs.Competitors.TotalCount.ShouldBe(1);
        inputs.Competitors.WellDocumentedCount.ShouldBe(expected);
    }

    [Fact]
    public async Task GetInputsAsync_PicksLatestMetricPerType_ByCreatedAt()
    {
        SeedStartup();
        _db.StartupMetrics.Add(StartupMetric.Create(_startupId, MetricType.Mrr, 100_000m, Now.AddDays(-2)));
        _db.StartupMetrics.Add(StartupMetric.Create(_startupId, MetricType.Mrr, 250_000m, Now)); // latest
        _db.StartupMetrics.Add(StartupMetric.Create(_startupId, MetricType.MomGrowth, 12m, Now));
        await _db.SaveChangesAsync();

        ScoringInputs inputs = (await CreateSut().GetInputsAsync(_startupId, CancellationToken.None)).Value;

        inputs.Traction.Mrr.ShouldBe(250_000m);
        inputs.Traction.MomGrowth.ShouldBe(12m);
        inputs.Traction.AnnualRecurringRevenue.ShouldBe(3_000_000m);
    }

    [Fact]
    public async Task GetInputsAsync_FallsBackToRevenueUsersGrowthRate_WhenPrimaryMetricsMissing()
    {
        SeedStartup();
        // No Mrr/Mau/MomGrowth — only the proxy metric types are present.
        _db.StartupMetrics.Add(StartupMetric.Create(_startupId, MetricType.Revenue, 80_000m, Now));
        _db.StartupMetrics.Add(StartupMetric.Create(_startupId, MetricType.Users, 4_000m, Now));
        _db.StartupMetrics.Add(StartupMetric.Create(_startupId, MetricType.GrowthRate, 15m, Now));
        await _db.SaveChangesAsync();

        ScoringInputs inputs = (await CreateSut().GetInputsAsync(_startupId, CancellationToken.None)).Value;

        inputs.Traction.Mrr.ShouldBe(80_000m);
        inputs.Traction.Mau.ShouldBe(4_000m);
        inputs.Traction.MomGrowth.ShouldBe(15m);
        // The Revenue proxy feeds the traction score but is flagged so it never annualizes into ARR.
        inputs.Traction.MrrIsProxy.ShouldBeTrue();
        inputs.Traction.AnnualRecurringRevenue.ShouldBe(0m);
    }

    [Fact]
    public async Task GetInputsAsync_PrefersPrimaryOverFallback_WhenBothPresent()
    {
        SeedStartup();
        _db.StartupMetrics.Add(StartupMetric.Create(_startupId, MetricType.Mrr, 500_000m, Now));
        _db.StartupMetrics.Add(StartupMetric.Create(_startupId, MetricType.Revenue, 1m, Now));
        await _db.SaveChangesAsync();

        ScoringInputs inputs = (await CreateSut().GetInputsAsync(_startupId, CancellationToken.None)).Value;

        inputs.Traction.Mrr.ShouldBe(500_000m);
        inputs.Traction.MrrIsProxy.ShouldBeFalse();
        inputs.Traction.AnnualRecurringRevenue.ShouldBe(6_000_000m);
    }

    [Fact]
    public async Task GetInputsAsync_DetectsArticulatedPositioningAndRoadmapCounts()
    {
        SeedStartup();
        _db.StartupProducts.Add(StartupProduct.Create(
            _startupId, "problem", "solution", null, "our value prop", "what sets us apart"));
        _db.StartupRoadmapItems.Add(StartupRoadmapItem.Create(_startupId, StartupStage.Seed, "M1", null, RoadmapItemStatus.Done, null, Now, Now));
        _db.StartupRoadmapItems.Add(StartupRoadmapItem.Create(_startupId, StartupStage.Seed, "M2", null, RoadmapItemStatus.InProgress, null, Now, Now));
        _db.StartupRoadmapItems.Add(StartupRoadmapItem.Create(_startupId, StartupStage.Seed, "M3", null, RoadmapItemStatus.Planned, null, Now, Now));
        await _db.SaveChangesAsync();

        ScoringInputs inputs = (await CreateSut().GetInputsAsync(_startupId, CancellationToken.None)).Value;

        inputs.Product.HasArticulatedPositioning.ShouldBeTrue();
        inputs.Roadmap.ItemCount.ShouldBe(3);
        inputs.Roadmap.DoneCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetInputsAsync_MapsValuationSignals_IndustryAndTargetAmount()
    {
        _db.Startups.Add(new Startup
        {
            Id = _startupId,
            Name = "Acme",
            PublicEmail = "acme@example.com",
            Stage = StartupStage.Seed,
            Industry = Industry.Saas,
            TargetRoundAmount = 50_000_000m,
            CreatedAt = Now,
            UpdatedAt = Now,
        });
        await _db.SaveChangesAsync();

        ScoringInputs inputs = (await CreateSut().GetInputsAsync(_startupId, CancellationToken.None)).Value;

        inputs.Industry.ShouldBe(Industry.Saas);
        inputs.TargetRoundAmount.ShouldBe(50_000_000m);
    }

    /// <summary>
    /// The partnership half of М3: the driver is the count of records that say what the arrangement
    /// is, and the total travels alongside it for transparency only. A record with no description is
    /// listed and counts for nothing — the same rule as an unanalysed competitor card.
    /// </summary>
    [Fact]
    public async Task GetInputsAsync_CountsOnlyWorkedOutPartnerships()
    {
        SeedStartup();
        _db.StartupPartnerships.AddRange(
            Partnership("Big Retailer", "https://retailer.example", "распространяет продукт в 40 точках"),
            Partnership("Университет", "https://uni.example", "   "),
            Partnership("Integrator", "https://integrator.example", null));
        await _db.SaveChangesAsync();

        ScoringInputs inputs = (await CreateSut().GetInputsAsync(_startupId, CancellationToken.None)).Value;

        inputs.Partnerships.TotalCount.ShouldBe(3);
        inputs.Partnerships.WorkedOutCount.ShouldBe(1);
    }

    private StartupPartnership Partnership(string name, string website, string? description) =>
        StartupPartnership.Create(
            _startupId, name, website, StartupPartnership.NormalizeDomain(website)!,
            PartnershipKind.Distribution, description, Now);

    [Fact]
    public async Task GetInputsAsync_DefaultsValuationSignals_WhenUnset()
    {
        SeedStartup();
        await _db.SaveChangesAsync();

        ScoringInputs inputs = (await CreateSut().GetInputsAsync(_startupId, CancellationToken.None)).Value;

        inputs.Industry.ShouldBe(Industry.Other);
        inputs.TargetRoundAmount.ShouldBeNull();
        inputs.Partnerships.ShouldBe(PartnershipSignals.None);
    }

    [Fact]
    public async Task GetInputsAsync_FlagsPositioningUnarticulated_WhenDifferentiatorsMissing()
    {
        SeedStartup();
        _db.StartupProducts.Add(StartupProduct.Create(
            _startupId, "problem", "solution", null, "our value prop", Differentiators: null));
        await _db.SaveChangesAsync();

        ScoringInputs inputs = (await CreateSut().GetInputsAsync(_startupId, CancellationToken.None)).Value;

        inputs.Product.HasArticulatedPositioning.ShouldBeFalse();
    }
}
