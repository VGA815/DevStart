using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.GetById;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Shouldly;

namespace DevStart.UnitTests.Application.Users.GetById;

public sealed class GetUserByIdQueryHandlerTests
{
    private readonly Guid _userId = Guid.NewGuid();

    private (GetUserByIdQueryHandler Sut, SpyFetchHandler Spy) CreateSut(Guid viewerId)
    {
        var spy = new SpyFetchHandler(new UserResponse
        {
            Id = _userId,
            Username = "acme",
            Email = "acme@example.com",
        });
        var sut = new GetUserByIdQueryHandler(spy, new TestUserContext(viewerId));
        return (sut, spy);
    }

    [Fact]
    public void PublicQuery_IsNotCacheable_ButFetchQueryIs()
    {
        // The viewer-independent fetch query carries the cache; the gated public query must not,
        // so the own-account gate can never be skipped on a cache hit.
        ((object)new GetUserByIdQuery(_userId) is ICacheableQuery).ShouldBeFalse();
        ((object)new FetchUserByIdQuery(_userId) is ICacheableQuery).ShouldBeTrue();
    }

    [Fact]
    public void FetchQuery_IsKeyedViewerIndependently()
    {
        new FetchUserByIdQuery(_userId).CacheKey.ShouldBe(CacheKeys.User(_userId));
    }

    [Fact]
    public async Task DifferentUser_IsRejected_WithoutTouchingTheCachedRead()
    {
        // Caller asks for another user's record. Even though the fetch (cache) holds that record,
        // the own-account gate rejects before delegation — closing the warm-cache disclosure.
        (GetUserByIdQueryHandler sut, SpyFetchHandler spy) = CreateSut(viewerId: Guid.NewGuid());

        Result<UserResponse> result =
            await sut.Handle(new GetUserByIdQuery(_userId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.Unauthorized");
        spy.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task OwnUser_ReceivesRecord()
    {
        (GetUserByIdQueryHandler sut, SpyFetchHandler spy) = CreateSut(viewerId: _userId);

        Result<UserResponse> result =
            await sut.Handle(new GetUserByIdQuery(_userId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.ShouldBe("acme@example.com");
        spy.CallCount.ShouldBe(1);
    }

    private sealed class SpyFetchHandler(UserResponse response)
        : IQueryHandler<FetchUserByIdQuery, UserResponse>
    {
        public int CallCount { get; private set; }

        public Task<Result<UserResponse>> Handle(FetchUserByIdQuery query, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(Result.Success(response));
        }
    }
}
