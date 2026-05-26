using DevStart.Application.StartupMetrics.GetAllByStartupId;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.Startups;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.StartupMetrics;

public sealed class GetStartupMetricsByStartupIdQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 26, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly Guid _startupId = Guid.NewGuid();

    public GetStartupMetricsByStartupIdQueryHandlerTests()
    {
        _db.Startups.Add(new Startup
        {
            Id = _startupId,
            Name = "Acme",
            PublicEmail = "acme@example.com",
            CreatedAt = Now,
            UpdatedAt = Now,
        });
        _db.StartupMetrics.Add(StartupMetric.Create(_startupId, MetricType.Users, 100m, Now));
        _db.StartupMetrics.Add(StartupMetric.Create(_startupId, MetricType.Mrr, 50m, Now));
        _db.SaveChanges();
    }

    private GetStartupMetricsByStartupIdQueryHandler CreateSut(Guid viewerId, bool hasPro) =>
        new(_db, new TestUserContext(viewerId), new StubSubscriptionChecker(hasPro));

    [Fact]
    public async Task NonMemberWithoutPro_DoesNotReceivePremiumMetrics()
    {
        GetStartupMetricsByStartupIdQueryHandler sut = CreateSut(Guid.NewGuid(), hasPro: false);

        Result<List<StartupMetricResponse>> result =
            await sut.Handle(new GetStartupMetricsByStartupIdQuery(_startupId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldHaveSingleItem().MetricType.ShouldBe(MetricType.Users);
    }

    [Fact]
    public async Task NonMemberWithPro_ReceivesPremiumMetrics()
    {
        GetStartupMetricsByStartupIdQueryHandler sut = CreateSut(Guid.NewGuid(), hasPro: true);

        Result<List<StartupMetricResponse>> result =
            await sut.Handle(new GetStartupMetricsByStartupIdQuery(_startupId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldContain(m => m.MetricType == MetricType.Mrr);
    }

    [Fact]
    public async Task Member_ReceivesAllMetricsRegardlessOfPro()
    {
        Guid viewerId = Guid.NewGuid();
        _db.StartupMembers.Add(StartupMember.Create(viewerId, _startupId, StartupRole.Founder, isPublic: true, Now));
        await _db.SaveChangesAsync();
        GetStartupMetricsByStartupIdQueryHandler sut = CreateSut(viewerId, hasPro: false);

        Result<List<StartupMetricResponse>> result =
            await sut.Handle(new GetStartupMetricsByStartupIdQuery(_startupId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
    }
}
