using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Shouldly;

namespace DevStart.UnitTests.Domain.Users;

public sealed class UserBanTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static User NewUser() => User.Create("u", "u@example.com", "hash", Now);

    [Fact]
    public void Ban_SetsFields_AndRaisesEvent()
    {
        User user = NewUser();
        Guid admin = Guid.NewGuid();

        Result result = user.Ban("spam", expiresAt: null, admin, Now);

        result.IsSuccess.ShouldBeTrue();
        user.IsBanned.ShouldBeTrue();
        user.BanReason.ShouldBe("spam");
        user.BannedByUserId.ShouldBe(admin);
        user.BanExpiresAt.ShouldBeNull();
        user.IsCurrentlyBanned(Now).ShouldBeTrue();
        user.DomainEvents.OfType<UserBannedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Ban_WhenAlreadyBanned_Fails()
    {
        User user = NewUser();
        user.Ban("a", null, Guid.NewGuid(), Now);

        Result second = user.Ban("b", null, Guid.NewGuid(), Now);

        second.IsFailure.ShouldBeTrue();
        second.Error.ShouldBe(UserErrors.AlreadyBanned);
    }

    [Fact]
    public void Ban_WithPastExpiry_Fails()
    {
        User user = NewUser();

        Result result = user.Ban("x", Now.AddMinutes(-1), Guid.NewGuid(), Now);

        result.Error.ShouldBe(UserErrors.BanExpiryInPast);
    }

    [Fact]
    public void TemporaryBan_IsLiftedLazily_AfterExpiry()
    {
        User user = NewUser();
        user.Ban("temp", Now.AddDays(1), Guid.NewGuid(), Now);

        user.IsCurrentlyBanned(Now).ShouldBeTrue();
        user.IsCurrentlyBanned(Now.AddDays(2)).ShouldBeFalse();
    }

    [Fact]
    public void Unban_ClearsFields_AndRaisesEvent()
    {
        User user = NewUser();
        user.Ban("x", null, Guid.NewGuid(), Now);
        user.ClearDomainEvents();

        Result result = user.Unban(Now);

        result.IsSuccess.ShouldBeTrue();
        user.IsBanned.ShouldBeFalse();
        user.BanReason.ShouldBeNull();
        user.BanExpiresAt.ShouldBeNull();
        user.DomainEvents.OfType<UserUnbannedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Unban_WhenNotBanned_Fails()
    {
        User user = NewUser();

        Result result = user.Unban(Now);

        result.Error.ShouldBe(UserErrors.NotBanned);
    }
}
