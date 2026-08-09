using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Configuration;
using DevStart.Application.Users.Security.UpdateSecuritySettings;
using DevStart.Domain.Security;
using DevStart.Domain.TrustedDevices;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.UnitTests.Application.Users.Security
{
    public class UpdateSecuritySettingsCommandHandlerTests
    {
        private const string Chrome =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();

        private async Task<User> SeedUserAsync(UserSystemRole role = UserSystemRole.User)
        {
            User user = User.Create("mila", $"mila{Guid.NewGuid():N}@example.com", "hash", _clock.UtcNow);
            user.IsVerified = true;
            user.Role = role;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        private UpdateSecuritySettingsCommandHandler CreateSut(Guid userId, TrustedDeviceOptions? options = null)
            => new(
                _db,
                new TestUserContext(userId),
                AuthTestKit.SecuritySettings(_db, _clock),
                AuthTestKit.TrustedDevices(_db, _clock, options),
                Options.Create(options ?? new TrustedDeviceOptions()),
                _clock);

        [Fact]
        public async Task Update_CreatesTheRow_WhenTheUserHasNeverSavedSettings()
        {
            User user = await SeedUserAsync();

            Result result = await CreateSut(user.Id).Handle(
                new UpdateSecuritySettingsCommand(TwoFactorStrictness.SameNetworkOnly, 14, false), default);

            Assert.True(result.IsSuccess);
            UserSecuritySettings stored = await _db.UserSecuritySettings.SingleAsync();
            Assert.Equal(TwoFactorStrictness.SameNetworkOnly, stored.Strictness);
            Assert.Equal(14, stored.TrustDurationDays);
            Assert.False(stored.NotifyOnNewDeviceLogin);
        }

        [Fact]
        public async Task Update_ClampsDuration_ToTheConfiguredCap()
        {
            User user = await SeedUserAsync();

            await CreateSut(user.Id, new TrustedDeviceOptions { MaxTrustDays = 30 }).Handle(
                new UpdateSecuritySettingsCommand(TwoFactorStrictness.RememberDevice, 90, true), default);

            Assert.Equal(30, (await _db.UserSecuritySettings.SingleAsync()).TrustDurationDays);
        }

        [Fact]
        public async Task Update_ClampsDuration_ToTheShorterAdminCap()
        {
            User admin = await SeedUserAsync(UserSystemRole.Admin);

            await CreateSut(admin.Id, new TrustedDeviceOptions { MaxTrustDays = 30, AdminMaxTrustDays = 7 }).Handle(
                new UpdateSecuritySettingsCommand(TwoFactorStrictness.RememberDevice, 30, true), default);

            Assert.Equal(7, (await _db.UserSecuritySettings.SingleAsync()).TrustDurationDays);
        }

        [Fact]
        public async Task ChangingStrictness_RevokesTrustedDevices()
        {
            User user = await SeedUserAsync();
            ITrustedDeviceService devices = AuthTestKit.TrustedDevices(_db, _clock);
            await devices.IssueAsync(user, null, Chrome, default);

            await CreateSut(user.Id).Handle(
                new UpdateSecuritySettingsCommand(TwoFactorStrictness.EveryLogin, 30, true), default);

            Assert.All(await _db.TrustedDevices.ToListAsync(), d => Assert.NotNull(d.RevokedAt));
        }

        [Fact]
        public async Task ChangingOnlyTheEmailToggle_LeavesTrustedDevicesAlone()
        {
            User user = await SeedUserAsync();
            await CreateSut(user.Id).Handle(
                new UpdateSecuritySettingsCommand(TwoFactorStrictness.RememberDevice, 30, true), default);
            await AuthTestKit.TrustedDevices(_db, _clock).IssueAsync(user, null, Chrome, default);

            await CreateSut(user.Id).Handle(
                new UpdateSecuritySettingsCommand(TwoFactorStrictness.RememberDevice, 30, false), default);

            // Only the trust policy invalidates devices; a notification preference is not one.
            TrustedDevice stored = await _db.TrustedDevices.SingleAsync();
            Assert.Null(stored.RevokedAt);
            Assert.False((await _db.UserSecuritySettings.SingleAsync()).NotifyOnNewDeviceLogin);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(45)]
        [InlineData(365)]
        public void Validator_RejectsDurationsOutsideThePresets(int days)
        {
            ValidationResult result = new UpdateSecuritySettingsCommandValidator().Validate(
                new UpdateSecuritySettingsCommand(TwoFactorStrictness.RememberDevice, days, true));

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validator_RejectsAnOutOfRangeStrictness()
        {
            ValidationResult result = new UpdateSecuritySettingsCommandValidator().Validate(
                new UpdateSecuritySettingsCommand((TwoFactorStrictness)99, 30, true));

            Assert.False(result.IsValid);
        }
    }
}
