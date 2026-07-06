using DevStart.Application.Users.TwoFactor.RegenerateRecoveryCodes;
using DevStart.Domain.TwoFactor;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace DevStart.UnitTests.Application.Users.TwoFactor
{
    public class RegenerateRecoveryCodesCommandHandlerTests
    {
        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();

        private RegenerateRecoveryCodesCommandHandler CreateSut(Guid userId) =>
            new(_db,
                new TestUserContext(userId),
                TwoFactorTestKit.CreateTotpProvider(),
                TwoFactorTestKit.CreateProtector(),
                TwoFactorTestKit.CreateRecoveryCodeGenerator(),
                _clock);

        private async Task<(User User, string Secret, string OldRecoveryCode)> SeedAsync()
        {
            User user = User.Create("rita", "rita@example.com", "hash", _clock.UtcNow);
            _db.Users.Add(user);
            (UserTwoFactor twoFactor, string secret) = TwoFactorTestKit.CreateEnabled(user.Id, _clock.UtcNow);
            _db.UserTwoFactors.Add(twoFactor);

            var generator = TwoFactorTestKit.CreateRecoveryCodeGenerator();
            string oldCode = generator.Generate(1).Single();
            _db.TwoFactorRecoveryCodes.Add(TwoFactorRecoveryCode.Create(user.Id, generator.Hash(oldCode), _clock.UtcNow));
            await _db.SaveChangesAsync();
            return (user, secret, oldCode);
        }

        [Fact]
        public async Task ValidTotp_ReplacesAllCodes()
        {
            (User user, string secret, string oldCode) = await SeedAsync();
            var generator = TwoFactorTestKit.CreateRecoveryCodeGenerator();

            Result<IReadOnlyList<string>> result = await CreateSut(user.Id).Handle(
                new RegenerateRecoveryCodesCommand(TwoFactorTestKit.CurrentCodeFor(secret)), default);

            Assert.True(result.IsSuccess);
            Assert.Equal(10, result.Value.Count);
            List<string> storedHashes = await _db.TwoFactorRecoveryCodes
                .Select(c => c.CodeHash).ToListAsync();
            Assert.Equal(10, storedHashes.Count);
            Assert.DoesNotContain(generator.Hash(oldCode), storedHashes);
        }

        [Fact]
        public async Task RecoveryCode_CannotMintNewCodes()
        {
            (User user, _, string oldCode) = await SeedAsync();

            Result<IReadOnlyList<string>> result = await CreateSut(user.Id).Handle(
                new RegenerateRecoveryCodesCommand(oldCode), default);

            Assert.Equal(TwoFactorErrors.InvalidCode, result.Error);
            Assert.Single(await _db.TwoFactorRecoveryCodes.ToListAsync());
        }

        [Fact]
        public async Task NotEnabled_ReturnsNotEnabled()
        {
            User user = User.Create("sam", "sam@example.com", "hash", _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            Result<IReadOnlyList<string>> result = await CreateSut(user.Id).Handle(
                new RegenerateRecoveryCodesCommand("123456"), default);

            Assert.Equal(TwoFactorErrors.NotEnabled, result.Error);
        }
    }
}
