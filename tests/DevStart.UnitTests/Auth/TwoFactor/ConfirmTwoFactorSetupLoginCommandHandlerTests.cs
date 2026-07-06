using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Auth.TwoFactor;
using DevStart.Application.Auth.TwoFactor.ConfirmSetupLogin;
using DevStart.Application.Auth.TwoFactor.SetupLogin;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Authentication.RefreshTokens;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.UnitTests.Auth.TwoFactor
{
    public class ConfirmTwoFactorSetupLoginCommandHandlerTests
    {
        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();
        private readonly InMemoryPendingTwoFactorStore _twoFactorStore = new();
        private readonly VerifyTwoFactorLoginCommandHandlerTests.InMemoryPendingRegistrationStore _pendingRegistrationStore = new();
        private readonly VerifyTwoFactorLoginCommandHandlerTests.FakeConsentService _consentService = new();
        private readonly TwoFactorEnrollmentService _enrollment;
        private readonly SetupTwoFactorLoginCommandHandler _setupSut;
        private readonly ConfirmTwoFactorSetupLoginCommandHandler _confirmSut;

        public ConfirmTwoFactorSetupLoginCommandHandlerTests()
        {
            _enrollment = new TwoFactorEnrollmentService(
                _db,
                TwoFactorTestKit.CreateTotpProvider(),
                TwoFactorTestKit.CreateProtector(),
                TwoFactorTestKit.CreateRecoveryCodeGenerator(),
                _clock);

            var refreshOptions = Options.Create(new RefreshTokenOptions { LifetimeDays = 30 });
            var refreshSvc = new RefreshTokenService(_db, _clock, refreshOptions);

            _setupSut = new SetupTwoFactorLoginCommandHandler(_db, _twoFactorStore, _enrollment, _clock);
            _confirmSut = new ConfirmTwoFactorSetupLoginCommandHandler(
                _db, _twoFactorStore, _pendingRegistrationStore, _enrollment,
                new StubTokenProvider(), refreshSvc, _consentService, _clock);
        }

        private async Task<(User Admin, string PendingToken)> SeedAdminWithSetupChallengeAsync()
        {
            User admin = User.Create("root", "root@example.com", "hash", _clock.UtcNow);
            admin.IsVerified = true;
            admin.Role = UserSystemRole.Admin;
            _db.Users.Add(admin);
            await _db.SaveChangesAsync();

            string token = "setup-" + Guid.NewGuid().ToString("N");
            await _twoFactorStore.SaveAsync(
                token, new PendingTwoFactorLogin(admin.Id, null, null, SetupRequired: true),
                TimeSpan.FromMinutes(5), default);
            return (admin, token);
        }

        [Fact]
        public async Task AdminBootstrap_SetupThenConfirm_EnablesAndIssuesTokens()
        {
            (User admin, string token) = await SeedAdminWithSetupChallengeAsync();

            Result<TwoFactorLoginSetupResponse> setup = await _setupSut.Handle(
                new SetupTwoFactorLoginCommand(token), default);
            Assert.True(setup.IsSuccess);
            Assert.Equal(token, setup.Value.PendingToken);
            Assert.Contains(setup.Value.Secret, setup.Value.OtpAuthUri);

            Result<TwoFactorSetupCompleteResponse> confirm = await _confirmSut.Handle(
                new ConfirmTwoFactorSetupLoginCommand(
                    token, TwoFactorTestKit.CurrentCodeFor(setup.Value.Secret), null, null),
                default);

            Assert.True(confirm.IsSuccess);
            Assert.Equal(10, confirm.Value.RecoveryCodes.Count);
            Assert.NotNull(confirm.Value.Auth.Tokens);
            Assert.Equal($"access-for-{admin.Id}", confirm.Value.Auth.Tokens!.AccessToken);

            UserTwoFactor stored = await _db.UserTwoFactors.SingleAsync();
            Assert.True(stored.IsEnabled);
            Assert.Equal(10, await _db.TwoFactorRecoveryCodes.CountAsync());
            Assert.Empty(_twoFactorStore.Items);
        }

        [Fact]
        public async Task Setup_CanBeCalledTwice_RotatingTheSecret()
        {
            (_, string token) = await SeedAdminWithSetupChallengeAsync();

            Result<TwoFactorLoginSetupResponse> first = await _setupSut.Handle(
                new SetupTwoFactorLoginCommand(token), default);
            Result<TwoFactorLoginSetupResponse> second = await _setupSut.Handle(
                new SetupTwoFactorLoginCommand(token), default);

            Assert.True(second.IsSuccess);
            Assert.NotEqual(first.Value.Secret, second.Value.Secret);

            // Only the latest secret confirms.
            Result<TwoFactorSetupCompleteResponse> confirm = await _confirmSut.Handle(
                new ConfirmTwoFactorSetupLoginCommand(
                    token, TwoFactorTestKit.CurrentCodeFor(second.Value.Secret), null, null),
                default);
            Assert.True(confirm.IsSuccess);
        }

        [Fact]
        public async Task Confirm_WithWrongCode_CountsAttempts_AndEventuallyKillsChallenge()
        {
            (_, string token) = await SeedAdminWithSetupChallengeAsync();
            Result<TwoFactorLoginSetupResponse> setup = await _setupSut.Handle(
                new SetupTwoFactorLoginCommand(token), default);
            string wrong = TwoFactorTestKit.WrongCodeFor(setup.Value.Secret);

            for (int attempt = 1; attempt <= 4; attempt++)
            {
                Result<TwoFactorSetupCompleteResponse> result = await _confirmSut.Handle(
                    new ConfirmTwoFactorSetupLoginCommand(token, wrong, null, null), default);
                Assert.Equal(TwoFactorErrors.InvalidCode, result.Error);
            }

            Result<TwoFactorSetupCompleteResponse> fifth = await _confirmSut.Handle(
                new ConfirmTwoFactorSetupLoginCommand(token, wrong, null, null), default);
            Assert.Equal(TwoFactorErrors.TooManyAttempts, fifth.Error);
            Assert.Empty(_twoFactorStore.Items);
        }

        [Fact]
        public async Task VerifyStyleToken_IsRejectedOnSetupEndpoints()
        {
            (User admin, _) = await SeedAdminWithSetupChallengeAsync();
            string verifyToken = "verify-" + Guid.NewGuid().ToString("N");
            await _twoFactorStore.SaveAsync(
                verifyToken, new PendingTwoFactorLogin(admin.Id, null, null, SetupRequired: false),
                TimeSpan.FromMinutes(5), default);

            Result<TwoFactorLoginSetupResponse> setup = await _setupSut.Handle(
                new SetupTwoFactorLoginCommand(verifyToken), default);
            Assert.Equal(TwoFactorErrors.ChallengeExpired, setup.Error);

            Result<TwoFactorSetupCompleteResponse> confirm = await _confirmSut.Handle(
                new ConfirmTwoFactorSetupLoginCommand(verifyToken, "123456", null, null), default);
            Assert.Equal(TwoFactorErrors.ChallengeExpired, confirm.Error);
        }
    }
}
