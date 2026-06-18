using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Scoring;
using DevStart.Application.Startups.GetScore;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Subscriptions;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.Startups.GetScore;

public sealed class GetStartupScoreQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 5, 26, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly Guid _startupId = Guid.NewGuid();

    public GetStartupScoreQueryHandlerTests()
    {
        _db.Startups.Add(new Startup
        {
            Id = _startupId,
            Name = "Acme",
            PublicEmail = "acme@example.com",
            Stage = StartupStage.Seed,
            CreatedAt = Now,
            UpdatedAt = Now,
        });
        _db.SaveChanges();
    }

    private (GetStartupScoreQueryHandler Sut, SpyComputeHandler Spy) CreateSut(Guid viewerId, bool hasPro)
    {
        var spy = new SpyComputeHandler();
        var sut = new GetStartupScoreQueryHandler(
            _db, spy, new TestUserContext(viewerId), new StubSubscriptionChecker(hasPro));
        return (sut, spy);
    }

    [Fact]
    public void PublicQuery_IsNotCacheable_ButComputeQueryIs()
    {
        // The viewer-independent compute query carries the cache; the gated public query must not,
        // so the Pro/member gate can never be skipped on a cache hit.
        ((object)new GetStartupScoreQuery(_startupId) is ICacheableQuery).ShouldBeFalse();
        ((object)new ComputeStartupScoreQuery(_startupId) is ICacheableQuery).ShouldBeTrue();
    }

    [Fact]
    public async Task NonMemberWithoutPro_IsRejected_WithoutTouchingTheComputation()
    {
        (GetStartupScoreQueryHandler sut, SpyComputeHandler spy) = CreateSut(Guid.NewGuid(), hasPro: false);

        Result<ScoreResult> result =
            await sut.Handle(new GetStartupScoreQuery(_startupId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SubscriptionErrors.ProRequired);
        // The gate runs before delegation, so the cached compute path is never reached for an
        // unauthorized viewer — closing the warm-cache paywall bypass.
        spy.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Member_ReceivesScore_RegardlessOfPro()
    {
        Guid viewerId = Guid.NewGuid();
        _db.StartupMembers.Add(StartupMember.Create(viewerId, _startupId, StartupRole.Founder, isPublic: true, Now));
        await _db.SaveChangesAsync();
        (GetStartupScoreQueryHandler sut, SpyComputeHandler spy) = CreateSut(viewerId, hasPro: false);

        Result<ScoreResult> result =
            await sut.Handle(new GetStartupScoreQuery(_startupId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        spy.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task NonMemberWithPro_ReceivesScore()
    {
        (GetStartupScoreQueryHandler sut, SpyComputeHandler spy) = CreateSut(Guid.NewGuid(), hasPro: true);

        Result<ScoreResult> result =
            await sut.Handle(new GetStartupScoreQuery(_startupId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        spy.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task MissingStartup_ReturnsNotFound()
    {
        (GetStartupScoreQueryHandler sut, SpyComputeHandler spy) = CreateSut(Guid.NewGuid(), hasPro: true);

        Result<ScoreResult> result =
            await sut.Handle(new GetStartupScoreQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Startups.NotFound");
        spy.CallCount.ShouldBe(0);
    }

    private sealed class SpyComputeHandler : IQueryHandler<ComputeStartupScoreQuery, ScoreResult>
    {
        public int CallCount { get; private set; }

        public Task<Result<ScoreResult>> Handle(ComputeStartupScoreQuery query, CancellationToken cancellationToken)
        {
            CallCount++;
            var score = new ScoreResult(
                TotalScore: 50m, TeamScore: 50m, MarketScore: 50m, ProductScore: 50m,
                TractionScore: 50m, CompetitionScore: 50m,
                ValuationLow: 1m, ValuationHigh: 2m,
                MethodsUsed: Array.Empty<string>(), CalculatedAt: Now);
            return Task.FromResult(Result.Success(score));
        }
    }
}
