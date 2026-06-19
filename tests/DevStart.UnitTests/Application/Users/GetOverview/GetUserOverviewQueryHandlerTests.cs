using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.GetOverview;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.Users.GetOverview;

public sealed class GetUserOverviewQueryHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();

    private (GetUserOverviewQueryHandler Sut, SpyFetchHandler Spy) CreateSut(Guid viewerId)
    {
        var spy = new SpyFetchHandler(Result.Success(new UserOverviewResponse
        {
            Id = _userId,
            Username = "acme",
            Email = "acme@example.com",
            Statistics = new UserStatisticsResponse
            {
                IsInvestor = true,
                CompletedDealsCount = 3,
                TotalInvestedAmount = 150_000m,
            }
        }));
        var sut = new GetUserOverviewQueryHandler(spy, new TestUserContext(viewerId));
        return (sut, spy);
    }

    [Fact]
    public void PublicQuery_IsNotCacheable_ButFetchQueryIs()
    {
        // The viewer-independent fetch query carries the cache; the public query must not, so the
        // viewer-dependent redaction can never be skipped on a cache hit.
        ((object)new GetUserOverviewQuery(_userId) is ICacheableQuery).ShouldBeFalse();
        ((object)new FetchUserOverviewQuery(_userId) is ICacheableQuery).ShouldBeTrue();
    }

    [Fact]
    public void FetchQuery_IsKeyedViewerIndependently()
    {
        new FetchUserOverviewQuery(_userId).CacheKey.ShouldBe(CacheKeys.UserOverview(_userId));
    }

    [Fact]
    public async Task Owner_ReceivesPrivateFields()
    {
        (GetUserOverviewQueryHandler sut, _) = CreateSut(viewerId: _userId);

        Result<UserOverviewResponse> result =
            await sut.Handle(new GetUserOverviewQuery(_userId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.ShouldBe("acme@example.com");
        result.Value.Statistics.TotalInvestedAmount.ShouldBe(150_000m);
        result.Value.Statistics.CompletedDealsCount.ShouldBe(3);
    }

    [Fact]
    public async Task NonOwner_HasPrivateFieldsRedacted_ButCountsRemain()
    {
        // A different viewer reads the cached full aggregate, but Email and TotalInvestedAmount are
        // redacted after the cache. Non-sensitive counters stay visible.
        (GetUserOverviewQueryHandler sut, _) = CreateSut(viewerId: Guid.NewGuid());

        Result<UserOverviewResponse> result =
            await sut.Handle(new GetUserOverviewQuery(_userId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.ShouldBeNull();
        result.Value.Statistics.TotalInvestedAmount.ShouldBeNull();
        result.Value.Statistics.CompletedDealsCount.ShouldBe(3);
        result.Value.Statistics.IsInvestor.ShouldBeTrue();
    }

    [Fact]
    public async Task Failure_FromFetch_IsPropagated()
    {
        var spy = new SpyFetchHandler(Result.Failure<UserOverviewResponse>(UserErrors.NotFound(_userId)));
        var sut = new GetUserOverviewQueryHandler(spy, new TestUserContext(Guid.NewGuid()));

        Result<UserOverviewResponse> result =
            await sut.Handle(new GetUserOverviewQuery(_userId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.NotFound");
    }

    private sealed class SpyFetchHandler(Result<UserOverviewResponse> result)
        : IQueryHandler<FetchUserOverviewQuery, UserOverviewResponse>
    {
        public int CallCount { get; private set; }

        public Task<Result<UserOverviewResponse>> Handle(FetchUserOverviewQuery query, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }
}
