using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Shouldly;

namespace DevStart.UnitTests.Domain.Startups;

public sealed class StartupBanTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Startup NewStartup() =>
        Startup.Create("Acme", "acme@example.com", null, null, default, null, null, null, Now, null, null);

    [Fact]
    public void Ban_SetsModerationFields_DistinctFromIsStopped()
    {
        Startup startup = NewStartup();
        Guid admin = Guid.NewGuid();

        Result result = startup.Ban("policy violation", expiresAt: null, admin, Now);

        result.IsSuccess.ShouldBeTrue();
        startup.IsBanned.ShouldBeTrue();
        startup.IsStopped.ShouldBeFalse();
        startup.BanReason.ShouldBe("policy violation");
        startup.BannedByUserId.ShouldBe(admin);
        startup.IsCurrentlyBanned(Now).ShouldBeTrue();
    }

    [Fact]
    public void Ban_WhenAlreadyBanned_Fails()
    {
        Startup startup = NewStartup();
        startup.Ban("a", null, Guid.NewGuid(), Now);

        Result second = startup.Ban("b", null, Guid.NewGuid(), Now);

        second.Error.ShouldBe(StartupErrors.AlreadyBanned);
    }

    [Fact]
    public void TemporaryBan_IsLiftedLazily_AfterExpiry()
    {
        Startup startup = NewStartup();
        startup.Ban("temp", Now.AddDays(3), Guid.NewGuid(), Now);

        startup.IsCurrentlyBanned(Now).ShouldBeTrue();
        startup.IsCurrentlyBanned(Now.AddDays(4)).ShouldBeFalse();
    }

    [Fact]
    public void Unban_ClearsFields()
    {
        Startup startup = NewStartup();
        startup.Ban("x", null, Guid.NewGuid(), Now);

        Result result = startup.Unban(Now);

        result.IsSuccess.ShouldBeTrue();
        startup.IsBanned.ShouldBeFalse();
        startup.BanReason.ShouldBeNull();
    }

    [Fact]
    public void Unban_WhenNotBanned_Fails()
    {
        Startup startup = NewStartup();

        Result result = startup.Unban(Now);

        result.Error.ShouldBe(StartupErrors.NotBanned);
    }
}
