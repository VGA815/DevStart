using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.UserPreferences.GetById;
using DevStart.Domain.UserPreferences;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.UserPreferences.GetById;

public sealed class GetUserPreferenceByIdQueryHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();

    private (GetUserPreferenceByIdQueryHandler Sut, SpyFetchHandler Spy) CreateSut(Guid viewerId)
    {
        var spy = new SpyFetchHandler(new UserPreferenceResponse
        {
            UserId = _userId,
            Theme = UserPreferenceTheme.Dark,
            ReceiveNotifications = true,
        });
        var sut = new GetUserPreferenceByIdQueryHandler(spy, new TestUserContext(viewerId));
        return (sut, spy);
    }

    [Fact]
    public void PublicQuery_IsNotCacheable_ButFetchQueryIs()
    {
        // The viewer-independent fetch query carries the cache; the gated public query must not,
        // so the own-account gate can never be skipped on a cache hit.
        ((object)new GetUserPreferenceByIdQuery(_userId) is ICacheableQuery).ShouldBeFalse();
        ((object)new FetchUserPreferenceByIdQuery(_userId) is ICacheableQuery).ShouldBeTrue();
    }

    [Fact]
    public void FetchQuery_IsKeyedViewerIndependently()
    {
        new FetchUserPreferenceByIdQuery(_userId).CacheKey.ShouldBe(CacheKeys.UserPreference(_userId));
    }

    [Fact]
    public async Task DifferentUser_IsRejected_WithoutTouchingTheCachedRead()
    {
        // Caller asks for another user's preferences. Even though the fetch (cache) holds them, the
        // own-account gate rejects before delegation — closing the warm-cache disclosure. NotFound
        // (not a distinct forbidden) keeps the response enumeration-safe.
        (GetUserPreferenceByIdQueryHandler sut, SpyFetchHandler spy) = CreateSut(viewerId: Guid.NewGuid());

        Result<UserPreferenceResponse> result =
            await sut.Handle(new GetUserPreferenceByIdQuery(_userId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("UserPreferences.NotFound");
        spy.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task OwnUser_ReceivesPreferences()
    {
        (GetUserPreferenceByIdQueryHandler sut, SpyFetchHandler spy) = CreateSut(viewerId: _userId);

        Result<UserPreferenceResponse> result =
            await sut.Handle(new GetUserPreferenceByIdQuery(_userId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Theme.ShouldBe(UserPreferenceTheme.Dark);
        spy.CallCount.ShouldBe(1);
    }

    private sealed class SpyFetchHandler(UserPreferenceResponse response)
        : IQueryHandler<FetchUserPreferenceByIdQuery, UserPreferenceResponse>
    {
        public int CallCount { get; private set; }

        public Task<Result<UserPreferenceResponse>> Handle(FetchUserPreferenceByIdQuery query, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(Result.Success(response));
        }
    }
}
