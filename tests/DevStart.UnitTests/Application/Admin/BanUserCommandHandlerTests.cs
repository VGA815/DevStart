using DevStart.Application.Admin.Users.BanUser;
using DevStart.Domain.Admin;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Authentication.RefreshTokens;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Application.Admin;

public sealed class BanUserCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
    private readonly FixedDateTimeProvider _clock = new() { UtcNow = Now };
    private readonly RefreshTokenService _refresh;
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly BanUserCommandHandler _sut;

    public BanUserCommandHandlerTests()
    {
        _refresh = new RefreshTokenService(_db, _clock, Options.Create(new RefreshTokenOptions { LifetimeDays = 30 }));
        _sut = new BanUserCommandHandler(_db, new TestUserContext(_adminId), _refresh, _clock);
    }

    private async Task<User> SeedUserAsync(UserSystemRole role = UserSystemRole.User)
    {
        User user = User.Create("target", "target@example.com", "hash", Now);
        user.Role = role;
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Ban_MarksBanned_RevokesSessions_AndWritesAudit()
    {
        User user = await SeedUserAsync();
        await _refresh.IssueAsync(user, "1.1.1.1", "ua", default);

        Result result = await _sut.Handle(new BanUserCommand(user.Id, "abuse", null), default);

        result.IsSuccess.ShouldBeTrue();

        User reloaded = await _db.Users.SingleAsync(u => u.Id == user.Id);
        reloaded.IsBanned.ShouldBeTrue();
        reloaded.BanReason.ShouldBe("abuse");
        reloaded.BannedByUserId.ShouldBe(_adminId);

        (await _db.RefreshTokens.CountAsync(t => t.UserId == user.Id && t.RevokedAt == null)).ShouldBe(0);

        AdminActionLog log = await _db.AdminActionLogs.SingleAsync();
        log.ActionType.ShouldBe(AdminActionType.BanUser);
        log.TargetType.ShouldBe(AdminTargetType.User);
        log.TargetId.ShouldBe(user.Id);
        log.AdminUserId.ShouldBe(_adminId);
    }

    [Fact]
    public async Task Ban_WithNoActiveSessions_StillPersistsBan()
    {
        User user = await SeedUserAsync();

        Result result = await _sut.Handle(new BanUserCommand(user.Id, "abuse", null), default);

        result.IsSuccess.ShouldBeTrue();
        (await _db.Users.SingleAsync(u => u.Id == user.Id)).IsBanned.ShouldBeTrue();
    }

    [Fact]
    public async Task Ban_Self_Fails()
    {
        Result result = await _sut.Handle(new BanUserCommand(_adminId, "x", null), default);

        result.Error.ShouldBe(UserErrors.CannotBanSelf);
    }

    [Fact]
    public async Task Ban_Admin_Fails()
    {
        User admin = await SeedUserAsync(UserSystemRole.Admin);

        Result result = await _sut.Handle(new BanUserCommand(admin.Id, "x", null), default);

        result.Error.ShouldBe(UserErrors.CannotBanAdmin);
    }

    [Fact]
    public async Task Ban_UnknownUser_ReturnsNotFound()
    {
        Guid missing = Guid.NewGuid();

        Result result = await _sut.Handle(new BanUserCommand(missing, "x", null), default);

        result.Error.ShouldBe(UserErrors.NotFound(missing));
    }
}
