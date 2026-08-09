using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Configuration;
using DevStart.Domain.Security;
using DevStart.Domain.TrustedDevices;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace DevStart.UnitTests.Auth.TrustedDevices
{
    public class TrustedDeviceServiceTests
    {
        private const string Chrome =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();

        private async Task<User> SeedUserAsync(UserSystemRole role = UserSystemRole.User)
        {
            User user = User.Create("dina", $"dina{Guid.NewGuid():N}@example.com", "hash", _clock.UtcNow);
            user.IsVerified = true;
            user.Role = role;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        private async Task SetStrictnessAsync(Guid userId, TwoFactorStrictness strictness, int trustDays = 30)
        {
            UserSecuritySettings settings = UserSecuritySettings.CreateDefault(userId, _clock.UtcNow);
            settings.Update(strictness, trustDays, notifyOnNewDeviceLogin: true, _clock.UtcNow);
            settings.ClearDomainEvents();
            _db.UserSecuritySettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        [Fact]
        public async Task IssueAsync_StoresHashedToken_NotRaw()
        {
            User user = await SeedUserAsync();
            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(_db, _clock);

            IssuedTrustedDevice? issued = await sut.IssueAsync(user, "203.0.113.7", Chrome, default);

            Assert.NotNull(issued);
            TrustedDevice stored = await _db.TrustedDevices.SingleAsync();
            Assert.NotEqual(issued.RawToken, stored.TokenHash);
            Assert.DoesNotContain(issued.RawToken, stored.TokenHash);
            Assert.Equal("Chrome на Windows", stored.Label);
        }

        [Fact]
        public async Task IssueAsync_CapsTrustDuration_AtConfiguredMaximum()
        {
            User user = await SeedUserAsync();
            await SetStrictnessAsync(user.Id, TwoFactorStrictness.RememberDevice, trustDays: 90);

            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(
                _db, _clock, new TrustedDeviceOptions { MaxTrustDays = 30 });

            IssuedTrustedDevice? issued = await sut.IssueAsync(user, null, Chrome, default);

            Assert.NotNull(issued);
            Assert.Equal(_clock.UtcNow.AddDays(30), issued.ExpiresAt);
        }

        [Fact]
        public async Task IssueAsync_AppliesShorterAdminCap()
        {
            User admin = await SeedUserAsync(UserSystemRole.Admin);
            await SetStrictnessAsync(admin.Id, TwoFactorStrictness.RememberDevice, trustDays: 30);

            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(
                _db, _clock, new TrustedDeviceOptions { MaxTrustDays = 30, AdminMaxTrustDays = 7 });

            IssuedTrustedDevice? issued = await sut.IssueAsync(admin, null, Chrome, default);

            Assert.NotNull(issued);
            Assert.Equal(_clock.UtcNow.AddDays(7), issued.ExpiresAt);
        }

        [Fact]
        public async Task IssueAsync_ReturnsNull_WhenUserDemandsCodeEveryLogin()
        {
            User user = await SeedUserAsync();
            await SetStrictnessAsync(user.Id, TwoFactorStrictness.EveryLogin);

            IssuedTrustedDevice? issued = await AuthTestKit.TrustedDevices(_db, _clock)
                .IssueAsync(user, null, Chrome, default);

            Assert.Null(issued);
            Assert.Empty(await _db.TrustedDevices.ToListAsync());
        }

        [Fact]
        public async Task IssueAsync_ReturnsNull_WhenFeatureIsDisabled()
        {
            User user = await SeedUserAsync();

            IssuedTrustedDevice? issued = await AuthTestKit
                .TrustedDevices(_db, _clock, new TrustedDeviceOptions { Enabled = false })
                .IssueAsync(user, null, Chrome, default);

            Assert.Null(issued);
        }

        [Fact]
        public async Task IssueAsync_EvictsLeastRecentlyUsed_WhenAtDeviceLimit()
        {
            User user = await SeedUserAsync();
            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(
                _db, _clock, new TrustedDeviceOptions { MaxDevicesPerUser = 2 });

            IssuedTrustedDevice? oldest = await sut.IssueAsync(user, null, Chrome, default);
            _clock.UtcNow = _clock.UtcNow.AddMinutes(1);
            await sut.IssueAsync(user, null, Chrome, default);
            _clock.UtcNow = _clock.UtcNow.AddMinutes(1);
            await sut.IssueAsync(user, null, Chrome, default);

            Assert.NotNull(oldest);
            TrustedDevice evicted = await _db.TrustedDevices.SingleAsync(d => d.Id == oldest.DeviceId);
            Assert.NotNull(evicted.RevokedAt);
            Assert.Equal(2, await _db.TrustedDevices.CountAsync(d => d.RevokedAt == null));
        }

        [Fact]
        public async Task TryConsumeAsync_AcceptsOwnActiveToken_AndTouchesIt()
        {
            User user = await SeedUserAsync();
            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(_db, _clock);
            IssuedTrustedDevice issued = (await sut.IssueAsync(user, "203.0.113.7", Chrome, default))!;

            _clock.UtcNow = _clock.UtcNow.AddDays(1);
            bool accepted = await sut.TryConsumeAsync(
                user, issued.RawToken, "203.0.113.9", TwoFactorStrictness.RememberDevice, default);

            Assert.True(accepted);
            TrustedDevice stored = await _db.TrustedDevices.SingleAsync();
            Assert.Equal(_clock.UtcNow, stored.LastUsedAt);
            Assert.Equal("203.0.113.9", stored.LastSeenIp);
        }

        [Fact]
        public async Task TryConsumeAsync_DoesNotExtendExpiry()
        {
            User user = await SeedUserAsync();
            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(_db, _clock);
            IssuedTrustedDevice issued = (await sut.IssueAsync(user, null, Chrome, default))!;

            _clock.UtcNow = _clock.UtcNow.AddDays(10);
            await sut.TryConsumeAsync(user, issued.RawToken, null, TwoFactorStrictness.RememberDevice, default);

            // Absolute expiry: using the device must never turn "30 days" into "forever".
            Assert.Equal(issued.ExpiresAt, (await _db.TrustedDevices.SingleAsync()).ExpiresAt);
        }

        [Fact]
        public async Task TryConsumeAsync_RejectsExpiredToken()
        {
            User user = await SeedUserAsync();
            await SetStrictnessAsync(user.Id, TwoFactorStrictness.RememberDevice, trustDays: 7);
            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(_db, _clock);
            IssuedTrustedDevice issued = (await sut.IssueAsync(user, null, Chrome, default))!;

            _clock.UtcNow = _clock.UtcNow.AddDays(8);

            Assert.False(await sut.TryConsumeAsync(
                user, issued.RawToken, null, TwoFactorStrictness.RememberDevice, default));
        }

        [Fact]
        public async Task TryConsumeAsync_RejectsRevokedToken()
        {
            User user = await SeedUserAsync();
            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(_db, _clock);
            IssuedTrustedDevice issued = (await sut.IssueAsync(user, null, Chrome, default))!;

            await sut.RevokeAllForUserAsync(user.Id, default);

            Assert.False(await sut.TryConsumeAsync(
                user, issued.RawToken, null, TwoFactorStrictness.RememberDevice, default));
        }

        [Fact]
        public async Task TryConsumeAsync_RejectsAndRevokes_WhenTokenBelongsToAnotherUser()
        {
            User owner = await SeedUserAsync();
            User attacker = await SeedUserAsync();
            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(_db, _clock);
            IssuedTrustedDevice issued = (await sut.IssueAsync(owner, null, Chrome, default))!;

            bool accepted = await sut.TryConsumeAsync(
                attacker, issued.RawToken, null, TwoFactorStrictness.RememberDevice, default);

            Assert.False(accepted);
            // A token surfacing under the wrong account is treated as compromised, not as a typo.
            Assert.NotNull((await _db.TrustedDevices.SingleAsync(d => d.Id == issued.DeviceId)).RevokedAt);
        }

        [Fact]
        public async Task TryConsumeAsync_IgnoresToken_UnderEveryLoginStrictness()
        {
            User user = await SeedUserAsync();
            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(_db, _clock);
            IssuedTrustedDevice issued = (await sut.IssueAsync(user, null, Chrome, default))!;

            Assert.False(await sut.TryConsumeAsync(
                user, issued.RawToken, null, TwoFactorStrictness.EveryLogin, default));
        }

        [Fact]
        public async Task TryConsumeAsync_UnderSameNetworkOnly_AcceptsSameSubnet_AndRejectsOtherWithoutRevoking()
        {
            User user = await SeedUserAsync();
            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(_db, _clock);
            IssuedTrustedDevice issued = (await sut.IssueAsync(user, "203.0.113.7", Chrome, default))!;

            Assert.True(await sut.TryConsumeAsync(
                user, issued.RawToken, "203.0.113.200", TwoFactorStrictness.SameNetworkOnly, default));

            Assert.False(await sut.TryConsumeAsync(
                user, issued.RawToken, "198.51.100.4", TwoFactorStrictness.SameNetworkOnly, default));

            // A different network today is not evidence of theft — the device survives.
            Assert.Null((await _db.TrustedDevices.SingleAsync()).RevokedAt);
        }

        [Fact]
        public async Task TryConsumeAsync_FailsClosed_OnAnUnrecognizedStrictness()
        {
            User user = await SeedUserAsync();
            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(_db, _clock);
            IssuedTrustedDevice issued = (await sut.IssueAsync(user, null, Chrome, default))!;

            // A value outside the enum — a corrupted row, or a newer member reaching an older
            // deployment mid-rollout. It must not fall through to the permissive branch.
            bool accepted = await sut.TryConsumeAsync(
                user, issued.RawToken, null, (TwoFactorStrictness)99, default);

            Assert.False(accepted);
        }

        [Fact]
        public async Task IssueAsync_FailsClosed_OnAnUnrecognizedStrictness()
        {
            User user = await SeedUserAsync();
            await SetStrictnessAsync(user.Id, (TwoFactorStrictness)99);

            IssuedTrustedDevice? issued = await AuthTestKit.TrustedDevices(_db, _clock)
                .IssueAsync(user, null, Chrome, default);

            Assert.Null(issued);
            Assert.Empty(await _db.TrustedDevices.ToListAsync());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("short")]
        [InlineData("contains spaces and punctuation!!!!!!!!!!!!!!!")]
        public async Task TryConsumeAsync_RejectsMalformedToken_WithoutTouchingTheDatabase(string? token)
        {
            User user = await SeedUserAsync();
            ITrustedDeviceService sut = AuthTestKit.TrustedDevices(_db, _clock);
            await sut.IssueAsync(user, null, Chrome, default);

            Assert.False(await sut.TryConsumeAsync(
                user, token, null, TwoFactorStrictness.RememberDevice, default));
        }
    }
}
