using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Auth.OAuth.Callback;
using DevStart.Domain.ExternalLogins;
using DevStart.Domain.RefreshTokens;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Authentication.RefreshTokens;
using DevStart.Infrastructure.Database;
using DevStart.SharedKernel;
using DevStart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.UnitTests.Auth.OAuth
{
    public class HandleOAuthCallbackCommandHandlerTests
    {
        private readonly ApplicationDbContext _db = InMemoryDbContextFactory.Create();
        private readonly InMemoryOAuthStateStore _stateStore = new();
        private readonly FakeExternalAuthProvider _provider = new() { Provider = ExternalLoginProvider.Google };
        private readonly FixedDateTimeProvider _clock = new();
        private readonly HandleOAuthCallbackCommandHandler _sut;

        public HandleOAuthCallbackCommandHandlerTests()
        {
            var refreshOptions = Options.Create(new RefreshTokenOptions { LifetimeDays = 30 });
            var refreshSvc = new RefreshTokenService(_db, _clock, refreshOptions);

            _sut = new HandleOAuthCallbackCommandHandler(
                _db,
                _stateStore,
                new FakeExternalAuthProviderFactory(_provider),
                new StubTokenProvider(),
                refreshSvc,
                _clock);
        }

        private async Task<string> SaveStateAsync(Guid? linkUserId = null)
        {
            string state = "test-state-" + Guid.NewGuid().ToString("N");
            await _stateStore.SaveAsync(
                state,
                new OAuthStateEntry(ExternalLoginProvider.Google, "verifier", "https://app/cb", linkUserId),
                TimeSpan.FromMinutes(5),
                default);
            return state;
        }

        [Fact]
        public async Task ExistingLogin_TouchesLastUsedAndIssuesTokens()
        {
            User user = User.Create("bob", "bob@example.com", "hash", _clock.UtcNow);
            _db.Users.Add(user);
            ExternalLogin link = ExternalLogin.Create(user.Id, ExternalLoginProvider.Google, "google-sub", "bob@example.com", _clock.UtcNow.AddDays(-1));
            _db.ExternalLogins.Add(link);
            await _db.SaveChangesAsync();

            _provider.Result = new ExternalUserInfo("google-sub", "bob@example.com", true, "Bob", null);
            string state = await SaveStateAsync();

            var cmd = new HandleOAuthCallbackCommand(ExternalLoginProvider.Google, "code", state, "ip", "ua");
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsSuccess);
            Assert.Equal($"access-for-{user.Id}", result.Value.AccessToken);
            ExternalLogin reloaded = await _db.ExternalLogins.SingleAsync();
            Assert.Equal(_clock.UtcNow, reloaded.LastUsedAt);
        }

        [Fact]
        public async Task InvalidState_ReturnsInvalidStateError()
        {
            _provider.Result = new ExternalUserInfo("x", "x@x", true, null, null);

            var cmd = new HandleOAuthCallbackCommand(ExternalLoginProvider.Google, "code", "never-stored", null, null);
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsFailure);
            Assert.Equal(ExternalLoginErrors.InvalidState, result.Error);
        }

        [Fact]
        public async Task StateForDifferentProvider_ReturnsInvalidStateError()
        {
            string state = "wrong-provider";
            await _stateStore.SaveAsync(
                state,
                new OAuthStateEntry(ExternalLoginProvider.GitHub, "v", "r", null),
                TimeSpan.FromMinutes(5), default);

            _provider.Result = new ExternalUserInfo("x", "x@x", true, null, null);

            var cmd = new HandleOAuthCallbackCommand(ExternalLoginProvider.Google, "code", state, null, null);
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsFailure);
            Assert.Equal(ExternalLoginErrors.InvalidState, result.Error);
        }

        [Fact]
        public async Task ExchangeThrows_ReturnsProviderError()
        {
            _provider.Throws = new InvalidOperationException("provider down");
            string state = await SaveStateAsync();

            var cmd = new HandleOAuthCallbackCommand(ExternalLoginProvider.Google, "code", state, null, null);
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsFailure);
            Assert.Equal(ExternalLoginErrors.ProviderError, result.Error);
        }

        [Fact]
        public async Task EmailMatchesUnverifiedLocalUser_ReturnsTypedError()
        {
            User user = User.Create("carol", "carol@example.com", "hash", _clock.UtcNow);
            user.IsVerified = false;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _provider.Result = new ExternalUserInfo("google-carol", "carol@example.com", true, "Carol", null);
            string state = await SaveStateAsync();

            var cmd = new HandleOAuthCallbackCommand(ExternalLoginProvider.Google, "c", state, null, null);
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsFailure);
            Assert.Equal(ExternalLoginErrors.EmailMatchesUnverifiedAccount, result.Error);
            Assert.Empty(await _db.ExternalLogins.ToListAsync());
        }

        [Fact]
        public async Task EmailMatchesVerifiedLocalUser_AutoLinks()
        {
            User user = User.Create("dave", "dave@example.com", "hash", _clock.UtcNow);
            user.IsVerified = true;
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _provider.Result = new ExternalUserInfo("google-dave", "dave@example.com", true, "Dave", null);
            string state = await SaveStateAsync();

            var cmd = new HandleOAuthCallbackCommand(ExternalLoginProvider.Google, "c", state, null, null);
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsSuccess);
            ExternalLogin link = await _db.ExternalLogins.SingleAsync();
            Assert.Equal(user.Id, link.UserId);
            Assert.Equal("google-dave", link.ProviderUserId);
        }

        [Fact]
        public async Task UnknownExternal_NoLocalUser_CreatesUserAndProfileAndLink()
        {
            _provider.Result = new ExternalUserInfo("google-eve", "eve@example.com", true, "Eve Doe", "https://avatar/eve");
            string state = await SaveStateAsync();

            var cmd = new HandleOAuthCallbackCommand(ExternalLoginProvider.Google, "c", state, null, null);
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsSuccess);

            User created = await _db.Users.SingleAsync();
            Assert.Equal("eve@example.com", created.Email);
            Assert.True(created.IsVerified);
            Assert.Null(created.PasswordHash);

            ExternalLogin link = await _db.ExternalLogins.SingleAsync();
            Assert.Equal(created.Id, link.UserId);

            Assert.NotNull(await _db.Profiles.SingleOrDefaultAsync(p => p.UserId == created.Id));
            Assert.NotNull(await _db.Preferences.SingleOrDefaultAsync(p => p.UserId == created.Id));
        }

        [Fact]
        public async Task NoEmailReturnedForNewUser_ReturnsEmailRequiredError()
        {
            _provider.Result = new ExternalUserInfo("anon", null, false, null, null);
            string state = await SaveStateAsync();

            var cmd = new HandleOAuthCallbackCommand(ExternalLoginProvider.Google, "c", state, null, null);
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsFailure);
            Assert.Equal(ExternalLoginErrors.EmailRequired, result.Error);
        }

        [Fact]
        public async Task LinkBranch_AddsLinkToCurrentUser()
        {
            User user = User.Create("frank", "frank@example.com", "hash", _clock.UtcNow);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _provider.Result = new ExternalUserInfo("google-frank", "frank@example.com", true, "Frank", null);
            string state = await SaveStateAsync(linkUserId: user.Id);

            var cmd = new HandleOAuthCallbackCommand(ExternalLoginProvider.Google, "c", state, null, null);
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsSuccess);
            ExternalLogin link = await _db.ExternalLogins.SingleAsync();
            Assert.Equal(user.Id, link.UserId);
        }

        [Fact]
        public async Task LinkBranch_ExternalAlreadyLinkedToAnotherUser_ReturnsConflict()
        {
            User userA = User.Create("a", "a@a.com", "h", _clock.UtcNow);
            User userB = User.Create("b", "b@b.com", "h", _clock.UtcNow);
            _db.Users.AddRange(userA, userB);
            ExternalLogin link = ExternalLogin.Create(userA.Id, ExternalLoginProvider.Google, "shared-sub", "a@a.com", _clock.UtcNow);
            _db.ExternalLogins.Add(link);
            await _db.SaveChangesAsync();

            _provider.Result = new ExternalUserInfo("shared-sub", "a@a.com", true, null, null);
            string state = await SaveStateAsync(linkUserId: userB.Id);

            var cmd = new HandleOAuthCallbackCommand(ExternalLoginProvider.Google, "c", state, null, null);
            Result<TokenPair> result = await _sut.Handle(cmd, default);

            Assert.True(result.IsFailure);
            Assert.Equal(ExternalLoginErrors.AlreadyLinkedToAnotherUser, result.Error);
        }
    }
}
