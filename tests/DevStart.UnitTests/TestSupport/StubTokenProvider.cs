using DevStart.Application.Abstractions.Authentication;
using DevStart.Domain.Users;

namespace DevStart.UnitTests.TestSupport
{
    internal sealed class StubTokenProvider : ITokenProvider
    {
        public int AccessTokenLifetimeSeconds => 3600;

        /// <summary>The sid the last access token was minted with, for tests that assert on it.</summary>
        public Guid? LastSessionId { get; private set; }

        // The shape stays "access-for-{id}" so callers can assert on it; the session id is recorded
        // on the side rather than baked into the string.
        public string CreateAccessToken(User user, Guid? sessionId = null)
        {
            LastSessionId = sessionId;
            return $"access-for-{user.Id}";
        }
    }
}
