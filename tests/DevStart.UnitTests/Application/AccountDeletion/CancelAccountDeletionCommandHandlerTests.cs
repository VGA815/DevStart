using DevStart.Application.AccountDeletion.CancelDeletion;
using DevStart.Domain.AccountDeletion;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.UnitTests.Application.AccountDeletion;

public sealed class CancelAccountDeletionCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new() { UtcNow = Now };
    private readonly Guid _userId = Guid.NewGuid();
    private readonly CancelAccountDeletionCommandHandler _sut;

    public CancelAccountDeletionCommandHandlerTests()
    {
        _sut = new CancelAccountDeletionCommandHandler(_db, new TestUserContext(_userId), _clock);
    }

    [Fact]
    public async Task Cancel_TakesTheAccountOffTheJobsList()
    {
        AccountDeletionRequest request = AccountDeletionRequest.Create(_userId, Now, TimeSpan.FromDays(7));
        _db.AccountDeletionRequests.Add(request);
        await _db.SaveChangesAsync();

        _clock.UtcNow = Now.AddDays(1);

        Result result = await _sut.Handle(new CancelAccountDeletionCommand(), default);

        result.IsSuccess.ShouldBeTrue();

        AccountDeletionRequest cancelled = await _db.AccountDeletionRequests.SingleAsync();
        cancelled.Status.ShouldBe(AccountDeletionRequestStatus.Cancelled);
        cancelled.CancelledAt.ShouldBe(Now.AddDays(1));
        cancelled.IsDue(Now.AddDays(30)).ShouldBeFalse();
    }

    [Fact]
    public async Task Cancel_WithNothingScheduled_ReportsNotRequested()
    {
        Result result = await _sut.Handle(new CancelAccountDeletionCommand(), default);

        result.Error.ShouldBe(AccountDeletionErrors.NotRequested);
    }

    [Fact]
    public async Task Cancel_DoesNotTouchSomeoneElsesRequest()
    {
        _db.AccountDeletionRequests.Add(
            AccountDeletionRequest.Create(Guid.NewGuid(), Now, TimeSpan.FromDays(7)));
        await _db.SaveChangesAsync();

        Result result = await _sut.Handle(new CancelAccountDeletionCommand(), default);

        result.Error.ShouldBe(AccountDeletionErrors.NotRequested);
        (await _db.AccountDeletionRequests.SingleAsync()).Status.ShouldBe(AccountDeletionRequestStatus.Pending);
    }
}
