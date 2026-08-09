using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Auth.OAuth;
using DevStart.Application.Auth.TwoFactor;
using DevStart.Application.Configuration;
using DevStart.Domain.Security;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.UnitTests.TestSupport;

namespace DevStart.UnitTests.Auth.TwoFactor
{
    public class TwoFactorLoginGateTests
    {
        private const string Chrome =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();
        private readonly InMemoryPendingTwoFactorStore _pendingStore = new();

        private async Task<User> SeedUserAsync(bool withTwoFactor, UserSystemRole role = UserSystemRole.User)
        {
            User user = User.Create("kira", $"kira{Guid.NewGuid():N}@example.com", "hash", _clock.UtcNow);
            user.IsVerified = true;
            user.Role = role;
            _db.Users.Add(user);

            if (withTwoFactor)
            {
                (UserTwoFactor twoFactor, _) = TwoFactorTestKit.CreateEnabled(user.Id, _clock.UtcNow);
                _db.UserTwoFactors.Add(twoFactor);
            }

            await _db.SaveChangesAsync();
            return user;
        }

        private async Task SetStrictnessAsync(Guid userId, TwoFactorStrictness strictness)
        {
            UserSecuritySettings settings = UserSecuritySettings.CreateDefault(userId, _clock.UtcNow);
            settings.Update(strictness, 30, notifyOnNewDeviceLogin: true, _clock.UtcNow);
            settings.ClearDomainEvents();
            _db.UserSecuritySettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        [Fact]
        public async Task NoTwoFactor_NonAdmin_PassesThrough()
        {
            User user = await SeedUserAsync(withTwoFactor: false);

            OAuthAuthResult? result = await AuthTestKit.Gate(_db, _pendingStore, _clock)
                .ChallengeIfRequiredAsync(user, null, Chrome, null, default);

            Assert.Null(result);
        }

        [Fact]
        public async Task TwoFactorEnabled_WithoutDeviceToken_Challenges()
        {
            User user = await SeedUserAsync(withTwoFactor: true);

            OAuthAuthResult? result = await AuthTestKit.Gate(_db, _pendingStore, _clock)
                .ChallengeIfRequiredAsync(user, null, Chrome, null, default);

            Assert.NotNull(result);
            Assert.NotNull(result.TwoFactor);
        }

        [Fact]
        public async Task TwoFactorEnabled_WithValidDeviceToken_SkipsChallenge()
        {
            User user = await SeedUserAsync(withTwoFactor: true);
            ITrustedDeviceService devices = AuthTestKit.TrustedDevices(_db, _clock);
            IssuedTrustedDevice issued = (await devices.IssueAsync(user, "203.0.113.7", Chrome, default))!;

            OAuthAuthResult? result = await AuthTestKit.Gate(_db, _pendingStore, _clock, devices)
                .ChallengeIfRequiredAsync(user, "203.0.113.7", Chrome, issued.RawToken, default);

            Assert.Null(result);
        }

        [Theory]
        [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")] // well-formed but unknown
        [InlineData("garbage")]                                     // malformed
        public async Task TwoFactorEnabled_WithBadDeviceToken_FallsBackToTheOrdinaryChallenge(string token)
        {
            User user = await SeedUserAsync(withTwoFactor: true);

            OAuthAuthResult? result = await AuthTestKit.Gate(_db, _pendingStore, _clock)
                .ChallengeIfRequiredAsync(user, null, Chrome, token, default);

            // No distinct outcome for a rejected device token — that would be an oracle.
            Assert.NotNull(result);
            Assert.NotNull(result.TwoFactor);
        }

        [Fact]
        public async Task EveryLoginStrictness_IgnoresValidDeviceToken()
        {
            User user = await SeedUserAsync(withTwoFactor: true);
            ITrustedDeviceService devices = AuthTestKit.TrustedDevices(_db, _clock);
            IssuedTrustedDevice issued = (await devices.IssueAsync(user, null, Chrome, default))!;

            await SetStrictnessAsync(user.Id, TwoFactorStrictness.EveryLogin);

            OAuthAuthResult? result = await AuthTestKit.Gate(_db, _pendingStore, _clock, devices)
                .ChallengeIfRequiredAsync(user, null, Chrome, issued.RawToken, default);

            Assert.NotNull(result);
            Assert.NotNull(result.TwoFactor);
        }

        [Fact]
        public async Task SameNetworkOnly_ChallengesFromADifferentSubnet()
        {
            User user = await SeedUserAsync(withTwoFactor: true);
            ITrustedDeviceService devices = AuthTestKit.TrustedDevices(_db, _clock);
            IssuedTrustedDevice issued = (await devices.IssueAsync(user, "203.0.113.7", Chrome, default))!;

            await SetStrictnessAsync(user.Id, TwoFactorStrictness.SameNetworkOnly);
            ITwoFactorLoginGate gate = AuthTestKit.Gate(_db, _pendingStore, _clock, devices);

            Assert.Null(await gate.ChallengeIfRequiredAsync(user, "203.0.113.99", Chrome, issued.RawToken, default));
            Assert.NotNull(await gate.ChallengeIfRequiredAsync(user, "198.51.100.4", Chrome, issued.RawToken, default));
        }

        [Fact]
        public async Task DisabledFeature_IgnoresValidDeviceToken()
        {
            User user = await SeedUserAsync(withTwoFactor: true);
            IssuedTrustedDevice issued =
                (await AuthTestKit.TrustedDevices(_db, _clock).IssueAsync(user, null, Chrome, default))!;

            ITrustedDeviceService offDevices = AuthTestKit.TrustedDevices(
                _db, _clock, new TrustedDeviceOptions { Enabled = false });

            OAuthAuthResult? result = await AuthTestKit.Gate(_db, _pendingStore, _clock, offDevices)
                .ChallengeIfRequiredAsync(user, null, Chrome, issued.RawToken, default);

            Assert.NotNull(result);
            Assert.NotNull(result.TwoFactor);
        }

        [Fact]
        public async Task AdminWithoutTwoFactor_StillMustEnroll_EvenWithAValidDeviceToken()
        {
            // The admin first enrolls, trusts this browser, then has 2FA reset. A device can only vouch
            // for a second factor the account currently has — otherwise the reset would be undone.
            User admin = await SeedUserAsync(withTwoFactor: true, UserSystemRole.Admin);
            ITrustedDeviceService devices = AuthTestKit.TrustedDevices(_db, _clock);
            IssuedTrustedDevice issued = (await devices.IssueAsync(admin, null, Chrome, default))!;

            _db.UserTwoFactors.RemoveRange(_db.UserTwoFactors);
            await _db.SaveChangesAsync();

            OAuthAuthResult? result = await AuthTestKit.Gate(_db, _pendingStore, _clock, devices)
                .ChallengeIfRequiredAsync(admin, null, Chrome, issued.RawToken, default);

            Assert.NotNull(result);
            Assert.NotNull(result.TwoFactorSetup);
            Assert.Null(result.Tokens);
        }
    }
}
