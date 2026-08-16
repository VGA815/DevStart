using DevStart.Application.AccountDeletion;
using DevStart.Application.AccountDeletion.GetStatus;
using DevStart.Application.AccountDeletion.RequestDeletion;
using DevStart.Domain.AccountDeletion;
using DevStart.Domain.Profiles;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.AccountDeletion;

public sealed class RequestAccountDeletionCommandHandlerTests
{
    private const string Password = "Password123!";
    private static readonly DateTime Now = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new() { UtcNow = Now };
    private readonly RecordingPasswordHasher _hasher = new();
    private readonly RecordingEmailSender _email = new();

    private RequestAccountDeletionCommandHandler CreateSut(Guid userId, int graceDays = 7) =>
        new(_db,
            new TestUserContext(userId),
            _hasher,
            _clock,
            Options.Create(new AccountDeletionOptions { GraceDays = graceDays }),
            _email);

    private async Task<User> SeedUserAsync(bool withPassword = true, UserSystemRole role = UserSystemRole.User)
    {
        User user = withPassword
            ? User.Create("leaver", "leaver@example.com", _hasher.Hash(Password), Now)
            : User.CreateExternal("leaver", "leaver@example.com", true, Now);
        user.Role = role;

        _db.Users.Add(user);
        _db.Profiles.Add(Profile.Create(user.Id, "Leaver", null, null, false, true, null));
        await _db.SaveChangesAsync();

        return user;
    }

    [Fact]
    public async Task Request_SchedulesErasureAfterTheGraceWindow_AndEmailsTheOwner()
    {
        User user = await SeedUserAsync();

        Result<AccountDeletionStatusResponse> result =
            await CreateSut(user.Id).Handle(new RequestAccountDeletionCommand(Password), default);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Pending.ShouldBeTrue();
        result.Value.ScheduledFor.ShouldBe(Now.AddDays(7));

        AccountDeletionRequest request = await _db.AccountDeletionRequests.SingleAsync();
        request.UserId.ShouldBe(user.Id);
        request.Status.ShouldBe(AccountDeletionRequestStatus.Pending);

        // The whole point of the window is that the owner hears about it out-of-band.
        _email.AccountDeletionNotices.ShouldHaveSingleItem();
        _email.AccountDeletionNotices[0].Email.ShouldBe(user.Email);
        _email.AccountDeletionNotices[0].ScheduledFor.ShouldBe(Now.AddDays(7));
    }

    [Fact]
    public async Task Request_LandsWellInsideTheThirtyDaysTheDocumentsPromise()
    {
        User user = await SeedUserAsync();

        Result<AccountDeletionStatusResponse> result =
            await CreateSut(user.Id).Handle(new RequestAccountDeletionCommand(Password), default);

        (result.Value.ScheduledFor!.Value - Now).TotalDays
            .ShouldBeLessThan(AccountDeletionOptions.PromisedMaxDays);
    }

    [Fact]
    public async Task Request_TellsTheUserWhichStartupsGoWithTheAccount()
    {
        User user = await SeedUserAsync();
        User coFounder = User.Create("co", "co@example.com", "hash", Now);
        _db.Users.Add(coFounder);
        _db.Profiles.Add(Profile.Create(coFounder.Id, "Co", null, null, false, true, null));

        Startup solo = Startup.Create("solo", "solo@x.test", null, null, StartupStage.Idea, null, null, null, Now, null, null);
        Startup shared = Startup.Create("shared", "shared@x.test", null, null, StartupStage.Idea, null, null, null, Now, null, null);
        _db.Startups.AddRange(solo, shared);
        _db.StartupMembers.Add(StartupMember.Create(user.Id, solo.Id, StartupRole.Founder, true, Now));
        _db.StartupMembers.Add(StartupMember.Create(user.Id, shared.Id, StartupRole.Founder, true, Now));
        _db.StartupMembers.Add(StartupMember.Create(coFounder.Id, shared.Id, StartupRole.Founder, true, Now));
        await _db.SaveChangesAsync();

        Result<AccountDeletionStatusResponse> result =
            await CreateSut(user.Id).Handle(new RequestAccountDeletionCommand(Password), default);

        result.Value.StartupsToDelete.ShouldHaveSingleItem();
        result.Value.StartupsToDelete[0].Id.ShouldBe(solo.Id);
        result.Value.StartupsToDelete[0].Name.ShouldBe("solo");
    }

    [Fact]
    public async Task Request_WithTheWrongPassword_IsRejected()
    {
        User user = await SeedUserAsync();

        Result<AccountDeletionStatusResponse> result =
            await CreateSut(user.Id).Handle(new RequestAccountDeletionCommand("not-the-password"), default);

        result.Error.ShouldBe(UserErrors.InvalidCurrentPassword);
        (await _db.AccountDeletionRequests.AnyAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task Request_WithNoPasswordSupplied_IsRejectedWhenTheAccountHasOne()
    {
        User user = await SeedUserAsync();

        Result<AccountDeletionStatusResponse> result =
            await CreateSut(user.Id).Handle(new RequestAccountDeletionCommand(null), default);

        result.Error.ShouldBe(UserErrors.InvalidCurrentPassword);
    }

    [Fact]
    public async Task Request_FromAnOAuthOnlyAccount_NeedsNoPassword()
    {
        User user = await SeedUserAsync(withPassword: false);

        Result<AccountDeletionStatusResponse> result =
            await CreateSut(user.Id).Handle(new RequestAccountDeletionCommand(null), default);

        result.IsSuccess.ShouldBeTrue();
        (await _db.AccountDeletionRequests.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Request_FromAnAdmin_IsRefused()
    {
        User admin = await SeedUserAsync(role: UserSystemRole.Admin);

        Result<AccountDeletionStatusResponse> result =
            await CreateSut(admin.Id).Handle(new RequestAccountDeletionCommand(Password), default);

        result.Error.ShouldBe(AccountDeletionErrors.AdminCannotSelfDelete);
    }

    [Fact]
    public async Task Request_Twice_DoesNotQueueTwoErasures()
    {
        User user = await SeedUserAsync();
        RequestAccountDeletionCommandHandler sut = CreateSut(user.Id);

        await sut.Handle(new RequestAccountDeletionCommand(Password), default);
        Result<AccountDeletionStatusResponse> second =
            await sut.Handle(new RequestAccountDeletionCommand(Password), default);

        second.Error.ShouldBe(AccountDeletionErrors.AlreadyRequested);
        (await _db.AccountDeletionRequests.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Request_AfterACancelledOne_IsAllowedAgain()
    {
        User user = await SeedUserAsync();
        AccountDeletionRequest cancelled = AccountDeletionRequest.Create(user.Id, Now.AddDays(-3), TimeSpan.FromDays(7));
        cancelled.Cancel(Now.AddDays(-2));
        _db.AccountDeletionRequests.Add(cancelled);
        await _db.SaveChangesAsync();

        Result<AccountDeletionStatusResponse> result =
            await CreateSut(user.Id).Handle(new RequestAccountDeletionCommand(Password), default);

        result.IsSuccess.ShouldBeTrue();
        (await _db.AccountDeletionRequests.CountAsync(r => r.Status == AccountDeletionRequestStatus.Pending)).ShouldBe(1);
    }
}
