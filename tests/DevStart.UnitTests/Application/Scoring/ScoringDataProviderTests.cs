using DevStart.Application.Scoring;
using DevStart.Domain.StartupCompetitors;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupMetrics;
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

    private IScoringDataProvider CreateSut() => new ScoringDataProvider(_db);

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
    public async Task GetInputsAsync_AssemblesStartupFieldsMembersAndCompetitorCount()
    {
        SeedStartup(StartupStage.Mvp, tam: 1_000_000_000m, sam: 500_000_000m, som: 100_000_000m);
        _db.StartupMembers.Add(StartupMember.Create(
            Guid.NewGuid(), _startupId, StartupRole.Founder, isPublic: true, Now,
            StartupPosition.CEO, yearsOfExperience: 5, hasPriorExit: true, previousStartupsCount: 2));
        _db.StartupCompetitors.Add(StartupCompetitor.Create(_startupId, "Rival", null, null, null, null, Now));
        _db.StartupCompetitors.Add(StartupCompetitor.Create(_startupId, "Rival2", null, null, null, null, Now));
        await _db.SaveChangesAsync();

        ScoringInputs inputs = (await CreateSut().GetInputsAsync(_startupId, CancellationToken.None)).Value;

        inputs.Stage.ShouldBe(StartupStage.Mvp);
        inputs.Tam.ShouldBe(1_000_000_000m);
        inputs.Sam.ShouldBe(500_000_000m);
        inputs.Som.ShouldBe(100_000_000m);
        inputs.HasPatents.ShouldBeTrue();
        inputs.CompetitorsCount.ShouldBe(2);
        inputs.Members.Count.ShouldBe(1);
        inputs.Members[0].HasPriorExit.ShouldBe(true);
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
