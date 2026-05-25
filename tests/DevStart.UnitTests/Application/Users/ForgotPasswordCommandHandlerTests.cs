using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Users.ForgotPassword;
using DevStart.Domain.PasswordResetTokens;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Authentication;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace DevStart.UnitTests.Application.Users
{
    public class ForgotPasswordCommandHandlerTests
    {
        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();
        private readonly RecordingEmailSender _email = new();
        private readonly IPasswordHasher _hasher = new PasswordHasher();
        private readonly ForgotPasswordCommandHandler _sut;

        public ForgotPasswordCommandHandlerTests()
        {
            _sut = new ForgotPasswordCommandHandler(_db, _email, _clock);
        }

        [Fact]
        public async Task ExistingUser_CreatesTokenAndSendsEmail()
        {
            User user = User.Create("greta", "greta@example.com", _hasher.Hash("S3cret!xx"), _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            Result result = await _sut.Handle(new ForgotPasswordCommand(user.Email), default);

            Assert.True(result.IsSuccess);
            PasswordResetToken token = await _db.PasswordResetTokens.SingleAsync();
            Assert.Equal(user.Id, token.UserId);
            Assert.Equal(_clock.UtcNow.AddMinutes(30), token.ExpiresAt);

            var sent = Assert.Single(_email.PasswordResets);
            Assert.Equal(user.Email, sent.Email);
            Assert.Equal(token.TokenId.ToString(), sent.Token);
        }

        [Fact]
        public async Task UnknownEmail_SucceedsWithoutSendingOrCreatingToken()
        {
            Result result = await _sut.Handle(new ForgotPasswordCommand("nobody@example.com"), default);

            Assert.True(result.IsSuccess);
            Assert.Empty(_email.PasswordResets);
            Assert.False(await _db.PasswordResetTokens.AnyAsync());
        }

        [Fact]
        public async Task RecentToken_IsThrottled_NoNewEmail()
        {
            User user = User.Create("ivy", "ivy@example.com", _hasher.Hash("S3cret!xx"), _clock.UtcNow);
            _db.Users.Add(user);
            _db.PasswordResetTokens.Add(
                PasswordResetToken.Create(user.Id, _clock.UtcNow, _clock.UtcNow.AddMinutes(30)));
            await _db.SaveChangesAsync();

            Result result = await _sut.Handle(new ForgotPasswordCommand(user.Email), default);

            Assert.True(result.IsSuccess);
            Assert.Empty(_email.PasswordResets);
            Assert.Equal(1, await _db.PasswordResetTokens.CountAsync());
        }
    }
}
