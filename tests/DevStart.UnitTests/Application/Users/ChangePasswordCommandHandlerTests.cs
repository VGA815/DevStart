using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Users.ChangePassword;
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
    public class ChangePasswordCommandHandlerTests
    {
        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();
        private readonly IPasswordHasher _hasher = new PasswordHasher();
        private readonly IRefreshTokenService _refresh;

        public ChangePasswordCommandHandlerTests()
        {
            var options = Options.Create(new RefreshTokenOptions { LifetimeDays = 30 });
            _refresh = AuthTestKit.RefreshTokens(_db, _clock);
        }

        private ChangePasswordCommandHandler CreateSut(Guid userId)
            => new(_db, new TestUserContext(userId), _hasher, _clock, _refresh);

        [Fact]
        public async Task CorrectCurrentPassword_ChangesPassword_RevokesSessions()
        {
            User user = User.Create("greta", "greta@example.com", _hasher.Hash("oldpass12"), _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            await _refresh.IssueAsync(user, null, null, default);

            Result result = await CreateSut(user.Id)
                .Handle(new ChangePasswordCommand("oldpass12", "newpass34"), default);

            Assert.True(result.IsSuccess);
            User updated = await _db.Users.SingleAsync();
            Assert.True(_hasher.Verify("newpass34", updated.PasswordHash!));

            List<RefreshToken> tokens = await _db.RefreshTokens.ToListAsync();
            Assert.NotEmpty(tokens);
            Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));
        }

        [Fact]
        public async Task WrongCurrentPassword_ReturnsInvalidCurrentPassword_LeavesPasswordUnchanged()
        {
            User user = User.Create("ivy", "ivy@example.com", _hasher.Hash("oldpass12"), _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            Result result = await CreateSut(user.Id)
                .Handle(new ChangePasswordCommand("wrongpass", "newpass34"), default);

            Assert.True(result.IsFailure);
            Assert.Equal(UserErrors.InvalidCurrentPassword, result.Error);
            Assert.True(_hasher.Verify("oldpass12", (await _db.Users.SingleAsync()).PasswordHash!));
        }

        [Fact]
        public async Task ExternalOnlyUser_ReturnsPasswordNotSet()
        {
            User user = User.CreateExternal("henry", "henry@example.com", true, _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            Result result = await CreateSut(user.Id)
                .Handle(new ChangePasswordCommand("anything", "newpass34"), default);

            Assert.True(result.IsFailure);
            Assert.Equal(UserErrors.PasswordNotSet, result.Error);
        }
    }
}
