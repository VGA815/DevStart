using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Admin.Users.ResetTwoFactor;
using DevStart.Domain.Admin;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Authentication.RefreshTokens;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.UnitTests.Application.Admin
{
    public class ResetUserTwoFactorCommandHandlerTests
    {
        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();
        private readonly IRefreshTokenService _refreshService;
        private readonly User _admin;

        public ResetUserTwoFactorCommandHandlerTests()
        {
            _refreshService = AuthTestKit.RefreshTokens(_db, _clock);

            _admin = User.Create("admin", "admin@example.com", "hash", _clock.UtcNow);
            _admin.Role = UserSystemRole.Admin;
            _db.Users.Add(_admin);
            _db.SaveChanges();
        }

        private ResetUserTwoFactorCommandHandler CreateSut() =>
            new(_db, new TestUserContext(_admin.Id), _refreshService, _clock);

        private async Task<User> SeedTargetWithTwoFactorAsync()
        {
            User target = User.Create("tanya", "tanya@example.com", "hash", _clock.UtcNow);
            _db.Users.Add(target);
            (UserTwoFactor twoFactor, _) = TwoFactorTestKit.CreateEnabled(target.Id, _clock.UtcNow);
            _db.UserTwoFactors.Add(twoFactor);
            var generator = TwoFactorTestKit.CreateRecoveryCodeGenerator();
            _db.TwoFactorRecoveryCodes.Add(
                TwoFactorRecoveryCode.Create(target.Id, generator.Hash("AAAA-BBBB-CC"), _clock.UtcNow));
            await _db.SaveChangesAsync();
            return target;
        }

        [Fact]
        public async Task Reset_WipesTwoFactor_RevokesSessions_AndWritesAudit()
        {
            User target = await SeedTargetWithTwoFactorAsync();
            await _refreshService.IssueAsync(target, null, null, default);

            Result result = await CreateSut().Handle(
                new ResetUserTwoFactorCommand(target.Id, "user lost device, identity verified via support"), default);

            Assert.True(result.IsSuccess);
            Assert.Empty(await _db.UserTwoFactors.ToListAsync());
            Assert.Empty(await _db.TwoFactorRecoveryCodes.ToListAsync());
            Assert.All(await _db.RefreshTokens.ToListAsync(), t => Assert.NotNull(t.RevokedAt));

            AdminActionLog audit = await _db.AdminActionLogs.SingleAsync();
            Assert.Equal(AdminActionType.ResetUserTwoFactor, audit.ActionType);
            Assert.Equal(AdminTargetType.User, audit.TargetType);
            Assert.Equal(target.Id, audit.TargetId);
            Assert.Equal(_admin.Id, audit.AdminUserId);
        }

        [Fact]
        public async Task SelfReset_IsRejected()
        {
            (UserTwoFactor twoFactor, _) = TwoFactorTestKit.CreateEnabled(_admin.Id, _clock.UtcNow);
            _db.UserTwoFactors.Add(twoFactor);
            await _db.SaveChangesAsync();

            Result result = await CreateSut().Handle(
                new ResetUserTwoFactorCommand(_admin.Id, "trying to reset my own"), default);

            Assert.Equal(TwoFactorErrors.CannotResetSelf, result.Error);
            Assert.Single(await _db.UserTwoFactors.ToListAsync());
        }

        [Fact]
        public async Task TargetWithoutTwoFactor_ReturnsNotEnabled()
        {
            User target = User.Create("uma", "uma@example.com", "hash", _clock.UtcNow);
            _db.Users.Add(target);
            await _db.SaveChangesAsync();

            Result result = await CreateSut().Handle(
                new ResetUserTwoFactorCommand(target.Id, "no 2fa to reset"), default);

            Assert.Equal(TwoFactorErrors.NotEnabled, result.Error);
        }
    }
}
