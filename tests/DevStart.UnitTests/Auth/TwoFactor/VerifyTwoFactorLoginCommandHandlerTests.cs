using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Auth.OAuth;
using DevStart.Application.Auth.TwoFactor;
using DevStart.Application.Auth.TwoFactor.VerifyLogin;
using DevStart.Application.UserConsents;
using DevStart.Application.Users.Register;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.UserConsents;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Authentication.RefreshTokens;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.UnitTests.Auth.TwoFactor
{
    public class VerifyTwoFactorLoginCommandHandlerTests
    {
        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();
        private readonly InMemoryPendingTwoFactorStore _twoFactorStore = new();
        private readonly InMemoryPendingRegistrationStore _pendingRegistrationStore = new();
        private readonly FakeConsentService _consentService = new();
        private readonly ITrustedDeviceService _trustedDevices;
        private readonly VerifyTwoFactorLoginCommandHandler _sut;

        public VerifyTwoFactorLoginCommandHandlerTests()
        {
            _trustedDevices = AuthTestKit.TrustedDevices(_db, _clock);
            _sut = new VerifyTwoFactorLoginCommandHandler(
                _db,
                _twoFactorStore,
                _pendingRegistrationStore,
                new TwoFactorCodeVerifier(
                    _db,
                    TwoFactorTestKit.CreateTotpProvider(),
                    TwoFactorTestKit.CreateProtector(),
                    TwoFactorTestKit.CreateRecoveryCodeGenerator(),
                    _clock),
                new StubTokenProvider(),
                AuthTestKit.RefreshTokens(_db, _clock, trustedDevices: _trustedDevices),
                _trustedDevices,
                _consentService,
                _clock);
        }

        private async Task<(User User, string Secret, string PendingToken)> SeedChallengedUserAsync()
        {
            User user = User.Create("vera", "vera@example.com", "hash", _clock.UtcNow);
            user.IsVerified = true;
            _db.Users.Add(user);
            (UserTwoFactor twoFactor, string secret) = TwoFactorTestKit.CreateEnabled(user.Id, _clock.UtcNow);
            _db.UserTwoFactors.Add(twoFactor);
            await _db.SaveChangesAsync();

            string token = "pending-" + Guid.NewGuid().ToString("N");
            await _twoFactorStore.SaveAsync(
                token, new PendingTwoFactorLogin(user.Id, "1.1.1.1", "ua", SetupRequired: false),
                TimeSpan.FromMinutes(5), default);
            return (user, secret, token);
        }

        [Fact]
        public async Task ValidTotp_IssuesTokens_AndConsumesChallenge()
        {
            (User user, string secret, string token) = await SeedChallengedUserAsync();

            var cmd = new VerifyTwoFactorLoginCommand(token, TwoFactorTestKit.CurrentCodeFor(secret), "1.1.1.1", "ua");
            Result<OAuthAuthResult> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.Tokens);
            Assert.Equal($"access-for-{user.Id}", result.Value.Tokens!.AccessToken);
            Assert.Empty(_twoFactorStore.Items);
        }

        [Fact]
        public async Task SameCodeTwice_SecondAttemptRejected()
        {
            (User user, string secret, string token) = await SeedChallengedUserAsync();
            string code = TwoFactorTestKit.CurrentCodeFor(secret);

            Result<OAuthAuthResult> first = await _sut.Handle(
                new VerifyTwoFactorLoginCommand(token, code, null, null), default);
            Assert.True(first.IsSuccess);

            // A sniffed code must not be reusable, even within its 30-second validity window.
            string secondToken = "pending-" + Guid.NewGuid().ToString("N");
            await _twoFactorStore.SaveAsync(
                secondToken, new PendingTwoFactorLogin(user.Id, null, null, SetupRequired: false),
                TimeSpan.FromMinutes(5), default);

            Result<OAuthAuthResult> second = await _sut.Handle(
                new VerifyTwoFactorLoginCommand(secondToken, code, null, null), default);

            Assert.True(second.IsFailure);
            Assert.Equal(TwoFactorErrors.InvalidCode, second.Error);
        }

        [Fact]
        public async Task RecoveryCode_Works_ButOnlyOnce()
        {
            (User user, _, string token) = await SeedChallengedUserAsync();
            var generator = TwoFactorTestKit.CreateRecoveryCodeGenerator();
            string recoveryCode = generator.Generate(1).Single();
            _db.TwoFactorRecoveryCodes.Add(
                TwoFactorRecoveryCode.Create(user.Id, generator.Hash(recoveryCode), _clock.UtcNow));
            await _db.SaveChangesAsync();

            Result<OAuthAuthResult> first = await _sut.Handle(
                new VerifyTwoFactorLoginCommand(token, recoveryCode, null, null), default);
            Assert.True(first.IsSuccess);
            Assert.NotNull(first.Value.Tokens);
            Assert.NotNull((await _db.TwoFactorRecoveryCodes.SingleAsync()).UsedAt);

            string secondToken = "pending-" + Guid.NewGuid().ToString("N");
            await _twoFactorStore.SaveAsync(
                secondToken, new PendingTwoFactorLogin(user.Id, null, null, SetupRequired: false),
                TimeSpan.FromMinutes(5), default);

            Result<OAuthAuthResult> second = await _sut.Handle(
                new VerifyTwoFactorLoginCommand(secondToken, recoveryCode, null, null), default);

            Assert.True(second.IsFailure);
            Assert.Equal(TwoFactorErrors.InvalidCode, second.Error);
        }

        [Fact]
        public async Task FifthWrongAttempt_InvalidatesChallenge()
        {
            (_, string secret, string token) = await SeedChallengedUserAsync();
            string wrong = TwoFactorTestKit.WrongCodeFor(secret);

            for (int attempt = 1; attempt <= 4; attempt++)
            {
                Result<OAuthAuthResult> result = await _sut.Handle(
                    new VerifyTwoFactorLoginCommand(token, wrong, null, null), default);
                Assert.Equal(TwoFactorErrors.InvalidCode, result.Error);
            }

            Result<OAuthAuthResult> fifth = await _sut.Handle(
                new VerifyTwoFactorLoginCommand(token, wrong, null, null), default);
            Assert.Equal(TwoFactorErrors.TooManyAttempts, fifth.Error);
            Assert.Empty(_twoFactorStore.Items);

            // The challenge is gone: even the right code no longer works.
            Result<OAuthAuthResult> after = await _sut.Handle(
                new VerifyTwoFactorLoginCommand(token, TwoFactorTestKit.CurrentCodeFor(secret), null, null), default);
            Assert.Equal(TwoFactorErrors.ChallengeExpired, after.Error);
        }

        [Fact]
        public async Task UnknownToken_ReturnsChallengeExpired()
        {
            Result<OAuthAuthResult> result = await _sut.Handle(
                new VerifyTwoFactorLoginCommand("missing", "123456", null, null), default);

            Assert.Equal(TwoFactorErrors.ChallengeExpired, result.Error);
        }

        [Fact]
        public async Task SetupToken_IsRejectedOnVerifyEndpoint()
        {
            (User user, string secret, _) = await SeedChallengedUserAsync();
            string setupToken = "setup-" + Guid.NewGuid().ToString("N");
            await _twoFactorStore.SaveAsync(
                setupToken, new PendingTwoFactorLogin(user.Id, null, null, SetupRequired: true),
                TimeSpan.FromMinutes(5), default);

            Result<OAuthAuthResult> result = await _sut.Handle(
                new VerifyTwoFactorLoginCommand(setupToken, TwoFactorTestKit.CurrentCodeFor(secret), null, null), default);

            Assert.Equal(TwoFactorErrors.SetupRequired, result.Error);
        }

        [Fact]
        public async Task BannedUser_IsRejected_AndChallengeDropped()
        {
            (User user, string secret, string token) = await SeedChallengedUserAsync();
            user.Ban("reason", null, Guid.NewGuid(), _clock.UtcNow);
            await _db.SaveChangesAsync();

            Result<OAuthAuthResult> result = await _sut.Handle(
                new VerifyTwoFactorLoginCommand(token, TwoFactorTestKit.CurrentCodeFor(secret), null, null), default);

            Assert.Equal(UserErrors.Banned, result.Error);
            Assert.Empty(_twoFactorStore.Items);
        }

        [Fact]
        public async Task OutdatedConsents_ReturnConsentChallenge_MarkedTwoFactorSatisfied()
        {
            (_, string secret, string token) = await SeedChallengedUserAsync();
            _consentService.MandatoryCurrent = false;

            Result<OAuthAuthResult> result = await _sut.Handle(
                new VerifyTwoFactorLoginCommand(token, TwoFactorTestKit.CurrentCodeFor(secret), null, null), default);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value.Tokens);
            Assert.NotNull(result.Value.Consent);
            PendingExternalRegistration pending = _pendingRegistrationStore.Items.Values.Single();
            Assert.True(pending.TwoFactorSatisfied);
        }

        [Fact]
        public async Task RememberDevice_MintsAGrant_AndStoresTheDevice()
        {
            (User user, string secret, string token) = await SeedChallengedUserAsync();

            Result<OAuthAuthResult> result = await _sut.Handle(
                new VerifyTwoFactorLoginCommand(
                    token, TwoFactorTestKit.CurrentCodeFor(secret), "1.1.1.1", "ua", RememberDevice: true),
                default);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.TrustedDevice);
            Assert.NotNull(result.Value.Tokens);

            DevStart.Domain.TrustedDevices.TrustedDevice stored = await _db.TrustedDevices.SingleAsync();
            Assert.Equal(user.Id, stored.UserId);
            Assert.Equal(result.Value.TrustedDevice!.DeviceId, stored.Id);
            // The raw token is handed out once and never persisted.
            Assert.NotEqual(result.Value.TrustedDevice.DeviceToken, stored.TokenHash);
        }

        [Fact]
        public async Task WithoutRememberDevice_NoDeviceIsTrusted()
        {
            (_, string secret, string token) = await SeedChallengedUserAsync();

            Result<OAuthAuthResult> result = await _sut.Handle(
                new VerifyTwoFactorLoginCommand(token, TwoFactorTestKit.CurrentCodeFor(secret), null, "ua"), default);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value.TrustedDevice);
            Assert.Empty(await _db.TrustedDevices.ToListAsync());
        }

        [Fact]
        public async Task RememberDevice_AlsoAppliesOnTheConsentBranch()
        {
            // The second factor is proven either way; an outstanding consent is an orthogonal concern.
            (_, string secret, string token) = await SeedChallengedUserAsync();
            _consentService.MandatoryCurrent = false;

            Result<OAuthAuthResult> result = await _sut.Handle(
                new VerifyTwoFactorLoginCommand(
                    token, TwoFactorTestKit.CurrentCodeFor(secret), null, "ua", RememberDevice: true),
                default);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.Consent);
            Assert.NotNull(result.Value.TrustedDevice);
        }

        internal sealed class InMemoryPendingRegistrationStore : IPendingRegistrationStore
        {
            public Dictionary<string, PendingExternalRegistration> Items { get; } = new();

            public Task SaveAsync(string token, PendingExternalRegistration entry, TimeSpan ttl, CancellationToken cancellationToken)
            {
                Items[token] = entry;
                return Task.CompletedTask;
            }

            public Task<PendingExternalRegistration?> ConsumeAsync(string token, CancellationToken cancellationToken)
            {
                Items.Remove(token, out PendingExternalRegistration? entry);
                return Task.FromResult(entry);
            }
        }

        internal sealed class FakeConsentService : IConsentService
        {
            public bool MandatoryCurrent { get; set; } = true;

            public Task<Result<List<UserConsent>>> BuildAcceptedConsentsAsync(
                Guid userId, IReadOnlyList<ConsentItem> consents, DateTime now, CancellationToken cancellationToken)
                => Task.FromResult(Result.Success(new List<UserConsent>()));

            public Task<bool> AreMandatoryConsentsCurrentAsync(Guid userId, CancellationToken cancellationToken)
                => Task.FromResult(MandatoryCurrent);

            public Task<IReadOnlyList<RequiredConsent>> GetRequiredConsentsAsync(CancellationToken cancellationToken)
                => Task.FromResult<IReadOnlyList<RequiredConsent>>(new List<RequiredConsent>());
        }
    }
}
