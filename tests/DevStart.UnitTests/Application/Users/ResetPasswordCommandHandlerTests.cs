using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Users.ResetPassword;
using DevStart.Domain.PasswordResetTokens;
using DevStart.Domain.RefreshTokens;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Authentication;
using DevStart.Infrastructure.Authentication.RefreshTokens;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.UnitTests.Application.Users
{
    public class ResetPasswordCommandHandlerTests
    {
        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();
        private readonly IPasswordHasher _hasher = new PasswordHasher();
        private readonly RefreshTokenService _refresh;
        private readonly ResetPasswordCommandHandler _sut;

        public ResetPasswordCommandHandlerTests()
        {
            var options = Options.Create(new RefreshTokenOptions { LifetimeDays = 30 });
            _refresh = new RefreshTokenService(_db, _clock, options);
            _sut = new ResetPasswordCommandHandler(_db, _hasher, _clock, _refresh);
        }

        [Fact]
        public async Task ValidToken_SetsNewPassword_RemovesToken_RevokesSessions()
        {
            User user = User.Create("greta", "greta@example.com", _hasher.Hash("oldpass12"), _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            await _refresh.IssueAsync(user, null, null, default);

            PasswordResetToken token = PasswordResetToken.Create(user.Id, _clock.UtcNow, _clock.UtcNow.AddMinutes(30));
            _db.PasswordResetTokens.Add(token);
            await _db.SaveChangesAsync();

            Result result = await _sut.Handle(new ResetPasswordCommand(token.TokenId, "newpass34"), default);

            Assert.True(result.IsSuccess);
            User updated = await _db.Users.SingleAsync();
            Assert.True(_hasher.Verify("newpass34", updated.PasswordHash!));
            Assert.False(await _db.PasswordResetTokens.AnyAsync());

            List<RefreshToken> tokens = await _db.RefreshTokens.ToListAsync();
            Assert.NotEmpty(tokens);
            Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));
        }

        [Fact]
        public async Task ExpiredToken_ReturnsNotFound_LeavesPasswordUnchanged()
        {
            User user = User.Create("ivy", "ivy@example.com", _hasher.Hash("oldpass12"), _clock.UtcNow);
            _db.Users.Add(user);
            PasswordResetToken token = PasswordResetToken.Create(
                user.Id, _clock.UtcNow.AddMinutes(-60), _clock.UtcNow.AddMinutes(-30));
            _db.PasswordResetTokens.Add(token);
            await _db.SaveChangesAsync();

            Result result = await _sut.Handle(new ResetPasswordCommand(token.TokenId, "newpass34"), default);

            Assert.True(result.IsFailure);
            Assert.Equal("PasswordResetTokens.NotFound", result.Error.Code);
            Assert.True(_hasher.Verify("oldpass12", (await _db.Users.SingleAsync()).PasswordHash!));
        }

        [Fact]
        public async Task UnknownToken_ReturnsNotFound()
        {
            Result result = await _sut.Handle(new ResetPasswordCommand(Guid.NewGuid(), "newpass34"), default);

            Assert.True(result.IsFailure);
            Assert.Equal("PasswordResetTokens.NotFound", result.Error.Code);
        }
    }
}
