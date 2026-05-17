using DevStart.Application.Abstractions.Authentication;
using DevStart.Domain.RefreshTokens;
using RefreshTokenEntity = DevStart.Domain.RefreshTokens.RefreshToken;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Authentication.RefreshTokens;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.UnitTests.Auth.RefreshToken
{
    public class RefreshTokenServiceTests
    {
        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly FixedDateTimeProvider _clock = new();
        private readonly RefreshTokenService _sut;
        private readonly User _user;

        public RefreshTokenServiceTests()
        {
            _user = User.Create("alice", "alice@example.com", "hash", _clock.UtcNow);
            _db.Users.Add(_user);
            _db.SaveChanges();

            var options = Options.Create(new RefreshTokenOptions { LifetimeDays = 30 });
            _sut = new RefreshTokenService(_db, _clock, options);
        }

        [Fact]
        public async Task IssueAsync_StoresHashedToken_NotRaw()
        {
            IssuedRefreshToken issued = await _sut.IssueAsync(_user, null, null, default);

            RefreshTokenEntity stored = await _db.RefreshTokens.SingleAsync();
            Assert.NotEqual(issued.RawToken, stored.TokenHash);
            Assert.Equal(RefreshTokenHasher.Hash(issued.RawToken), stored.TokenHash);
            Assert.Equal(_user.Id, stored.UserId);
            Assert.Equal(_clock.UtcNow.AddDays(30), stored.ExpiresAt);
        }

        [Fact]
        public async Task RotateAsync_Success_RevokesOldAndIssuesNew()
        {
            IssuedRefreshToken first = await _sut.IssueAsync(_user, "1.2.3.4", "ua", default);

            _clock.UtcNow = _clock.UtcNow.AddMinutes(5);

            Result<RotatedTokens> result = await _sut.RotateAsync(first.RawToken, "5.6.7.8", "ua2", default);

            Assert.True(result.IsSuccess);
            Assert.NotEqual(first.RawToken, result.Value.RawRefreshToken);

            List<RefreshTokenEntity> all = await _db.RefreshTokens.OrderBy(x => x.CreatedAt).ToListAsync();
            Assert.Equal(2, all.Count);
            Assert.NotNull(all[0].RevokedAt);
            Assert.Equal(all[1].Id, all[0].ReplacedByTokenId);
            Assert.Null(all[1].RevokedAt);
        }

        [Fact]
        public async Task RotateAsync_UnknownToken_ReturnsInvalid()
        {
            Result<RotatedTokens> result = await _sut.RotateAsync("not-a-real-token", null, null, default);

            Assert.True(result.IsFailure);
            Assert.Equal(RefreshTokenErrors.Invalid, result.Error);
        }

        [Fact]
        public async Task RotateAsync_ExpiredToken_ReturnsExpired()
        {
            IssuedRefreshToken issued = await _sut.IssueAsync(_user, null, null, default);

            _clock.UtcNow = _clock.UtcNow.AddDays(31);

            Result<RotatedTokens> result = await _sut.RotateAsync(issued.RawToken, null, null, default);

            Assert.True(result.IsFailure);
            Assert.Equal(RefreshTokenErrors.Expired, result.Error);
        }

        [Fact]
        public async Task RotateAsync_ReuseOfAlreadyRevokedToken_RevokesAllUserTokens()
        {
            IssuedRefreshToken first = await _sut.IssueAsync(_user, null, null, default);
            IssuedRefreshToken parallel = await _sut.IssueAsync(_user, null, null, default);

            Result<RotatedTokens> firstRotation = await _sut.RotateAsync(first.RawToken, null, null, default);
            Assert.True(firstRotation.IsSuccess);

            Result<RotatedTokens> reuseAttempt = await _sut.RotateAsync(first.RawToken, null, null, default);

            Assert.True(reuseAttempt.IsFailure);
            Assert.Equal(RefreshTokenErrors.ReuseDetected, reuseAttempt.Error);

            List<RefreshTokenEntity> tokens = await _db.RefreshTokens.ToListAsync();
            Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));
        }

        [Fact]
        public async Task RevokeAsync_MarksTokenRevoked()
        {
            IssuedRefreshToken issued = await _sut.IssueAsync(_user, null, null, default);

            Result result = await _sut.RevokeAsync(issued.RawToken, default);

            Assert.True(result.IsSuccess);
            RefreshTokenEntity token = await _db.RefreshTokens.SingleAsync();
            Assert.NotNull(token.RevokedAt);
        }

        [Fact]
        public async Task RevokeAllForUserAsync_RevokesEveryActiveToken()
        {
            await _sut.IssueAsync(_user, null, null, default);
            await _sut.IssueAsync(_user, null, null, default);
            await _sut.IssueAsync(_user, null, null, default);

            await _sut.RevokeAllForUserAsync(_user.Id, default);

            List<RefreshTokenEntity> tokens = await _db.RefreshTokens.ToListAsync();
            Assert.Equal(3, tokens.Count);
            Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));
        }
    }
}
