using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Auth.TwoFactor;
using DevStart.Application.Users.TwoFactor.Enable;
using DevStart.Application.Users.TwoFactor.Setup;
using DevStart.Domain.RefreshTokens;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Authentication.RefreshTokens;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.UnitTests.Application.Users.TwoFactor
{
    public class EnableTwoFactorCommandHandlerTests
    {
        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();
        private readonly TwoFactorEnrollmentService _enrollment;
        private readonly IRefreshTokenService _refreshService;
        private readonly User _user;

        public EnableTwoFactorCommandHandlerTests()
        {
            _enrollment = new TwoFactorEnrollmentService(
                _db,
                TwoFactorTestKit.CreateTotpProvider(),
                TwoFactorTestKit.CreateProtector(),
                TwoFactorTestKit.CreateRecoveryCodeGenerator(),
                _clock);
            _refreshService = AuthTestKit.RefreshTokens(_db, _clock);

            _user = User.Create("nina", "nina@example.com", "hash", _clock.UtcNow);
            _user.IsVerified = true;
            _db.Users.Add(_user);
            _db.SaveChanges();
        }

        private SetupTwoFactorCommandHandler SetupSut() =>
            new(_db, new TestUserContext(_user.Id), _enrollment);

        private EnableTwoFactorCommandHandler EnableSut() =>
            new(new TestUserContext(_user.Id), _enrollment, _refreshService);

        [Fact]
        public async Task SetupThenEnable_ActivatesAndReturnsTenCodes_AndRevokesSessions()
        {
            await _refreshService.IssueAsync(_user, "1.1.1.1", "ua", default);

            Result<TwoFactorSetupData> setup = await SetupSut().Handle(new SetupTwoFactorCommand(), default);
            Assert.True(setup.IsSuccess);

            Result<IReadOnlyList<string>> enabled = await EnableSut().Handle(
                new EnableTwoFactorCommand(TwoFactorTestKit.CurrentCodeFor(setup.Value.Secret)), default);

            Assert.True(enabled.IsSuccess);
            Assert.Equal(10, enabled.Value.Count);

            UserTwoFactor stored = await _db.UserTwoFactors.SingleAsync();
            Assert.True(stored.IsEnabled);
            // The secret must not be stored in plaintext.
            Assert.DoesNotContain(setup.Value.Secret, stored.EncryptedSecret);
            // Credential change revokes all sessions.
            Assert.All(await _db.RefreshTokens.ToListAsync(), t => Assert.NotNull(t.RevokedAt));
        }

        [Fact]
        public async Task Enable_WithWrongCode_Fails()
        {
            Result<TwoFactorSetupData> setup = await SetupSut().Handle(new SetupTwoFactorCommand(), default);

            Result<IReadOnlyList<string>> result = await EnableSut().Handle(
                new EnableTwoFactorCommand(TwoFactorTestKit.WrongCodeFor(setup.Value.Secret)), default);

            Assert.Equal(TwoFactorErrors.InvalidCode, result.Error);
            Assert.False((await _db.UserTwoFactors.SingleAsync()).IsEnabled);
        }

        [Fact]
        public async Task Enable_WithoutSetup_ReturnsSetupNotStarted()
        {
            Result<IReadOnlyList<string>> result = await EnableSut().Handle(
                new EnableTwoFactorCommand("123456"), default);

            Assert.Equal(TwoFactorErrors.SetupNotStarted, result.Error);
        }

        [Fact]
        public async Task SetupOrEnable_WhenAlreadyEnabled_ReturnsAlreadyEnabled()
        {
            (UserTwoFactor twoFactor, string secret) = TwoFactorTestKit.CreateEnabled(_user.Id, _clock.UtcNow);
            _db.UserTwoFactors.Add(twoFactor);
            await _db.SaveChangesAsync();

            Result<TwoFactorSetupData> setup = await SetupSut().Handle(new SetupTwoFactorCommand(), default);
            Assert.Equal(TwoFactorErrors.AlreadyEnabled, setup.Error);

            Result<IReadOnlyList<string>> enable = await EnableSut().Handle(
                new EnableTwoFactorCommand(TwoFactorTestKit.CurrentCodeFor(secret)), default);
            Assert.Equal(TwoFactorErrors.AlreadyEnabled, enable.Error);
        }
    }
}
