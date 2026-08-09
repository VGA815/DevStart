using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Auth.OAuth;
using DevStart.Application.Auth.TwoFactor;
using DevStart.Application.UserConsents;
using DevStart.Application.Users.Login;
using DevStart.Application.Users.Register;
using DevStart.Domain.UserConsents;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Authentication;
using DevStart.Infrastructure.Authentication.RefreshTokens;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.Extensions.Options;

namespace DevStart.UnitTests.Auth.Users
{
    public class LoginUserCommandHandlerTests
    {
        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();
        private readonly IPasswordHasher _hasher = new PasswordHasher();
        private readonly FakeConsentService _consentService = new();
        private readonly InMemoryPendingRegistrationStore _pendingStore = new();
        private readonly InMemoryPendingTwoFactorStore _twoFactorStore = new();
        private readonly LoginUserCommandHandler _sut;

        public LoginUserCommandHandlerTests()
        {
            _sut = new LoginUserCommandHandler(
                _db, _hasher, new StubTokenProvider(), AuthTestKit.RefreshTokens(_db, _clock),
                _consentService, _pendingStore,
                AuthTestKit.Gate(_db, _twoFactorStore, _clock), _clock);
        }

        [Fact]
        public async Task ValidCredentials_ReturnsTokenPair()
        {
            string password = "S3cret!";
            User user = User.Create("greta", "greta@example.com", _hasher.Hash(password), _clock.UtcNow);
            user.IsVerified = true;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var cmd = new LoginUserCommand(user.Email, password, "1.1.1.1", "ua");
            Result<OAuthAuthResult> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.Tokens);
            Assert.Equal($"access-for-{user.Id}", result.Value.Tokens!.AccessToken);
            Assert.False(string.IsNullOrEmpty(result.Value.Tokens.RefreshToken));
            Assert.Equal(3600, result.Value.Tokens.ExpiresIn);
        }

        [Fact]
        public async Task OutdatedConsents_ReturnsConsentChallenge()
        {
            string password = "S3cret!";
            User user = User.Create("kara", "kara@example.com", _hasher.Hash(password), _clock.UtcNow);
            user.IsVerified = true;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            _consentService.MandatoryCurrent = false;

            var cmd = new LoginUserCommand(user.Email, password, null, null);
            Result<OAuthAuthResult> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value.Tokens);
            Assert.NotNull(result.Value.Consent);
            Assert.Single(_pendingStore.Items);
            Assert.Equal(user.Id, _pendingStore.Items.Values.Single().ExistingUserId);
        }

        [Fact]
        public async Task ExternalOnlyUser_NoPassword_ReturnsNotFound()
        {
            User user = User.CreateExternal("henry", "henry@example.com", true, _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var cmd = new LoginUserCommand(user.Email, "anything", null, null);
            Result<OAuthAuthResult> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsFailure);
            Assert.Equal(UserErrors.NotFoundByEmail, result.Error);
        }

        [Fact]
        public async Task UnverifiedEmail_ReturnsEmailNotVerified()
        {
            string password = "S3cret!";
            User user = User.Create("jack", "jack@example.com", _hasher.Hash(password), _clock.UtcNow);
            // IsVerified is false by default
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var cmd = new LoginUserCommand(user.Email, password, null, null);
            Result<OAuthAuthResult> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsFailure);
            Assert.Equal(UserErrors.EmailNotVerified, result.Error);
        }

        [Fact]
        public async Task WrongPassword_ReturnsNotFoundByEmail()
        {
            User user = User.Create("ivy", "ivy@example.com", _hasher.Hash("right"), _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var cmd = new LoginUserCommand(user.Email, "wrong", null, null);
            Result<OAuthAuthResult> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsFailure);
            Assert.Equal(UserErrors.NotFoundByEmail, result.Error);
        }

        [Fact]
        public async Task TwoFactorEnabledUser_GetsTwoFactorChallenge_NotTokens()
        {
            string password = "S3cret!";
            User user = User.Create("mila", "mila@example.com", _hasher.Hash(password), _clock.UtcNow);
            user.IsVerified = true;
            _db.Users.Add(user);
            (DevStart.Domain.TwoFactor.UserTwoFactor twoFactor, _) = TwoFactorTestKit.CreateEnabled(user.Id, _clock.UtcNow);
            _db.UserTwoFactors.Add(twoFactor);
            await _db.SaveChangesAsync();

            var cmd = new LoginUserCommand(user.Email, password, "1.1.1.1", "ua");
            Result<OAuthAuthResult> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value.Tokens);
            Assert.Null(result.Value.Consent);
            Assert.NotNull(result.Value.TwoFactor);
            var pending = _twoFactorStore.Items.Values.Single();
            Assert.Equal(user.Id, pending.UserId);
            Assert.False(pending.SetupRequired);
        }

        [Fact]
        public async Task AdminWithoutTwoFactor_GetsSetupChallenge()
        {
            string password = "S3cret!";
            User admin = User.Create("boss", "boss@example.com", _hasher.Hash(password), _clock.UtcNow);
            admin.IsVerified = true;
            admin.Role = UserSystemRole.Admin;
            _db.Users.Add(admin);
            await _db.SaveChangesAsync();

            var cmd = new LoginUserCommand(admin.Email, password, null, null);
            Result<OAuthAuthResult> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsSuccess);
            Assert.Null(result.Value.Tokens);
            Assert.NotNull(result.Value.TwoFactorSetup);
            var pending = _twoFactorStore.Items.Values.Single();
            Assert.Equal(admin.Id, pending.UserId);
            Assert.True(pending.SetupRequired);
        }

        [Fact]
        public async Task UnknownEmail_StillInvokesVerify_ToEqualizeTiming()
        {
            // No user is seeded. The verifier must still run (against a dummy hash) so the response time
            // doesn't reveal whether the email is registered — and the result mirrors a wrong password.
            var recordingHasher = new RecordingPasswordHasher();
            var sut = new LoginUserCommandHandler(
                _db, recordingHasher, new StubTokenProvider(), AuthTestKit.RefreshTokens(_db, _clock),
                _consentService, _pendingStore, AuthTestKit.Gate(_db, _twoFactorStore, _clock), _clock);

            var cmd = new LoginUserCommand("nobody@example.com", "whatever", null, null);
            Result<OAuthAuthResult> result = await sut.Handle(cmd, default);

            Assert.True(result.IsFailure);
            Assert.Equal(UserErrors.NotFoundByEmail, result.Error);
            Assert.Equal(1, recordingHasher.VerifyCallCount);
        }

        private sealed class InMemoryPendingRegistrationStore : IPendingRegistrationStore
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

        private sealed class FakeConsentService : IConsentService
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
