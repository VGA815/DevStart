using DevStart.Application.Auth.TwoFactor;
using DevStart.Application.Users.TwoFactor.Disable;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Authentication;
using DevStart.Infrastructure.Authentication.RefreshTokens;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.UnitTests.Application.Users.TwoFactor
{
    public class DisableTwoFactorCommandHandlerTests
    {
        private const string Password = "S3cret!pass";

        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();
        private readonly PasswordHasher _hasher = new();
        private readonly RefreshTokenService _refreshService;

        public DisableTwoFactorCommandHandlerTests()
        {
            _refreshService = new RefreshTokenService(
                _db, _clock, Options.Create(new RefreshTokenOptions { LifetimeDays = 30 }));
        }

        private DisableTwoFactorCommandHandler CreateSut(Guid userId) =>
            new(_db,
                new TestUserContext(userId),
                _hasher,
                new TwoFactorCodeVerifier(
                    _db,
                    TwoFactorTestKit.CreateTotpProvider(),
                    TwoFactorTestKit.CreateProtector(),
                    TwoFactorTestKit.CreateRecoveryCodeGenerator(),
                    _clock),
                _refreshService);

        private async Task<(User User, string Secret)> SeedUserWithTwoFactorAsync(bool withPassword = true)
        {
            User user = withPassword
                ? User.Create("olga", "olga@example.com", _hasher.Hash(Password), _clock.UtcNow)
                : User.CreateExternal("olga", "olga@example.com", true, _clock.UtcNow);
            user.IsVerified = true;
            _db.Users.Add(user);
            (UserTwoFactor twoFactor, string secret) = TwoFactorTestKit.CreateEnabled(user.Id, _clock.UtcNow);
            _db.UserTwoFactors.Add(twoFactor);
            await _db.SaveChangesAsync();
            return (user, secret);
        }

        [Fact]
        public async Task PasswordAndTotp_DisablesAndWipesCodes_AndRevokesSessions()
        {
            (User user, string secret) = await SeedUserWithTwoFactorAsync();
            var generator = TwoFactorTestKit.CreateRecoveryCodeGenerator();
            _db.TwoFactorRecoveryCodes.Add(TwoFactorRecoveryCode.Create(user.Id, generator.Hash("AAAA-BBBB-CC"), _clock.UtcNow));
            await _db.SaveChangesAsync();
            await _refreshService.IssueAsync(user, null, null, default);

            Result result = await CreateSut(user.Id).Handle(
                new DisableTwoFactorCommand(Password, TwoFactorTestKit.CurrentCodeFor(secret)), default);

            Assert.True(result.IsSuccess);
            Assert.Empty(await _db.UserTwoFactors.ToListAsync());
            Assert.Empty(await _db.TwoFactorRecoveryCodes.ToListAsync());
            Assert.All(await _db.RefreshTokens.ToListAsync(), t => Assert.NotNull(t.RevokedAt));
        }

        [Fact]
        public async Task WrongPassword_IsRejected_EvenWithValidCode()
        {
            (User user, string secret) = await SeedUserWithTwoFactorAsync();

            Result result = await CreateSut(user.Id).Handle(
                new DisableTwoFactorCommand("wrong-password", TwoFactorTestKit.CurrentCodeFor(secret)), default);

            Assert.Equal(UserErrors.InvalidCurrentPassword, result.Error);
            Assert.Single(await _db.UserTwoFactors.ToListAsync());
        }

        [Fact]
        public async Task MissingPassword_IsRejected_ForPasswordAccounts()
        {
            (User user, string secret) = await SeedUserWithTwoFactorAsync();

            Result result = await CreateSut(user.Id).Handle(
                new DisableTwoFactorCommand(null, TwoFactorTestKit.CurrentCodeFor(secret)), default);

            Assert.Equal(UserErrors.InvalidCurrentPassword, result.Error);
        }

        [Fact]
        public async Task OAuthOnlyAccount_DisablesWithCodeAlone()
        {
            (User user, string secret) = await SeedUserWithTwoFactorAsync(withPassword: false);

            Result result = await CreateSut(user.Id).Handle(
                new DisableTwoFactorCommand(null, TwoFactorTestKit.CurrentCodeFor(secret)), default);

            Assert.True(result.IsSuccess);
            Assert.Empty(await _db.UserTwoFactors.ToListAsync());
        }

        [Fact]
        public async Task RecoveryCode_IsAcceptedAsSecondFactor()
        {
            (User user, _) = await SeedUserWithTwoFactorAsync();
            var generator = TwoFactorTestKit.CreateRecoveryCodeGenerator();
            string recoveryCode = generator.Generate(1).Single();
            _db.TwoFactorRecoveryCodes.Add(TwoFactorRecoveryCode.Create(user.Id, generator.Hash(recoveryCode), _clock.UtcNow));
            await _db.SaveChangesAsync();

            Result result = await CreateSut(user.Id).Handle(
                new DisableTwoFactorCommand(Password, recoveryCode), default);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task NotEnabled_ReturnsNotEnabled()
        {
            User user = User.Create("pete", "pete@example.com", _hasher.Hash(Password), _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            Result result = await CreateSut(user.Id).Handle(
                new DisableTwoFactorCommand(Password, "123456"), default);

            Assert.Equal(TwoFactorErrors.NotEnabled, result.Error);
        }
    }
}
