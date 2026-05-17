using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Users.Login;
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
        private readonly LoginUserCommandHandler _sut;

        public LoginUserCommandHandlerTests()
        {
            var refreshOptions = Options.Create(new RefreshTokenOptions { LifetimeDays = 30 });
            var refreshSvc = new RefreshTokenService(_db, _clock, refreshOptions);
            _sut = new LoginUserCommandHandler(_db, _hasher, new StubTokenProvider(), refreshSvc);
        }

        [Fact]
        public async Task ValidCredentials_ReturnsTokenPair()
        {
            string password = "S3cret!";
            User user = User.Create("greta", "greta@example.com", _hasher.Hash(password), _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var cmd = new LoginUserCommand(user.Email, password, "1.1.1.1", "ua");
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsSuccess);
            Assert.Equal($"access-for-{user.Id}", result.Value.AccessToken);
            Assert.False(string.IsNullOrEmpty(result.Value.RefreshToken));
            Assert.Equal(3600, result.Value.ExpiresIn);
        }

        [Fact]
        public async Task ExternalOnlyUser_NoPassword_ReturnsNotFound()
        {
            User user = User.CreateExternal("henry", "henry@example.com", true, _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var cmd = new LoginUserCommand(user.Email, "anything", null, null);
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsFailure);
            Assert.Equal(UserErrors.NotFoundByEmail, result.Error);
        }

        [Fact]
        public async Task WrongPassword_ReturnsNotFoundByEmail()
        {
            User user = User.Create("ivy", "ivy@example.com", _hasher.Hash("right"), _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var cmd = new LoginUserCommand(user.Email, "wrong", null, null);
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsFailure);
            Assert.Equal(UserErrors.NotFoundByEmail, result.Error);
        }
    }
}
