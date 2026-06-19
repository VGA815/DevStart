using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupMetrics.GetById;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.Startups;
using DevStart.Domain.Subscriptions;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.StartupMetrics.GetById;

public sealed class GetStartupMetricByIdQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 26, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly Guid _startupId = Guid.NewGuid();
    private readonly Guid _metricId = Guid.NewGuid();

    private StartupMetricResponse Metric(MetricType type) => new()
    {
        Id = _metricId,
        StartupId = _startupId,
        MetricType = type,
        Value = 50m,
        CreatedAt = Now,
    };

    private (GetStartupMetricByIdQueryHandler Sut, SpyFetchHandler Spy) CreateSut(
        Result<StartupMetricResponse> fetchResult, Guid viewerId, bool hasPro)
    {
        var spy = new SpyFetchHandler(fetchResult);
        var sut = new GetStartupMetricByIdQueryHandler(
            _db, spy, new TestUserContext(viewerId), new StubSubscriptionChecker(hasPro));
        return (sut, spy);
    }

    [Fact]
    public void PublicQuery_IsNotCacheable_ButFetchQueryIs()
    {
        // The viewer-independent fetch query carries the cache; the gated public query must not,
        // so the premium Pro/member gate can never be skipped on a cache hit.
        ((object)new GetStartupMetricByIdQuery(_metricId) is ICacheableQuery).ShouldBeFalse();
        ((object)new FetchStartupMetricByIdQuery(_metricId) is ICacheableQuery).ShouldBeTrue();
    }

    [Fact]
    public void FetchQuery_IsKeyedViewerIndependently()
    {
        new FetchStartupMetricByIdQuery(_metricId).CacheKey.ShouldBe(CacheKeys.StartupMetric(_metricId));
    }

    [Fact]
    public async Task NonMemberWithoutPro_IsRejected_ForPremiumMetric_EvenWhenCached()
    {
        // The fetch handler returns a successful premium metric, simulating a value that is already
        // sitting in the cache (warmed by an earlier member/Pro request). The gate must still reject
        // an unauthorized viewer and never hand back the cached premium value — this is the bug.
        (GetStartupMetricByIdQueryHandler sut, SpyFetchHandler spy) =
            CreateSut(Result.Success(Metric(MetricType.Mrr)), Guid.NewGuid(), hasPro: false);

        Result<StartupMetricResponse> result =
            await sut.Handle(new GetStartupMetricByIdQuery(_metricId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SubscriptionErrors.ProRequired);
        // The cached value was available (the fetch ran), but the gate withheld it from the caller.
        spy.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task NonMemberWithPro_ReceivesPremiumMetric()
    {
        (GetStartupMetricByIdQueryHandler sut, _) =
            CreateSut(Result.Success(Metric(MetricType.Mrr)), Guid.NewGuid(), hasPro: true);

        Result<StartupMetricResponse> result =
            await sut.Handle(new GetStartupMetricByIdQuery(_metricId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MetricType.ShouldBe(MetricType.Mrr);
    }

    [Fact]
    public async Task Member_ReceivesPremiumMetric_RegardlessOfPro()
    {
        Guid viewerId = Guid.NewGuid();
        _db.StartupMembers.Add(StartupMember.Create(viewerId, _startupId, StartupRole.Founder, isPublic: true, Now));
        await _db.SaveChangesAsync();
        (GetStartupMetricByIdQueryHandler sut, _) =
            CreateSut(Result.Success(Metric(MetricType.Mrr)), viewerId, hasPro: false);

        Result<StartupMetricResponse> result =
            await sut.Handle(new GetStartupMetricByIdQuery(_metricId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MetricType.ShouldBe(MetricType.Mrr);
    }

    [Fact]
    public async Task NonPremiumMetric_IsReturned_WithoutGate()
    {
        (GetStartupMetricByIdQueryHandler sut, _) =
            CreateSut(Result.Success(Metric(MetricType.Users)), Guid.NewGuid(), hasPro: false);

        Result<StartupMetricResponse> result =
            await sut.Handle(new GetStartupMetricByIdQuery(_metricId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MetricType.ShouldBe(MetricType.Users);
    }

    [Fact]
    public async Task MissingMetric_ReturnsNotFound()
    {
        (GetStartupMetricByIdQueryHandler sut, _) = CreateSut(
            Result.Failure<StartupMetricResponse>(StartupMetricErrors.NotFound(_metricId)),
            Guid.NewGuid(), hasPro: true);

        Result<StartupMetricResponse> result =
            await sut.Handle(new GetStartupMetricByIdQuery(_metricId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("StartupMetrics.NotFound");
    }

    private sealed class SpyFetchHandler(Result<StartupMetricResponse> result)
        : IQueryHandler<FetchStartupMetricByIdQuery, StartupMetricResponse>
    {
        public int CallCount { get; private set; }

        public Task<Result<StartupMetricResponse>> Handle(FetchStartupMetricByIdQuery query, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }
}
