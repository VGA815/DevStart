using DevStart.Application.Abstractions.Authentication;
using DevStart.Domain.Users;

namespace DevStart.UnitTests.TestSupport
{
    internal sealed class StubTokenProvider : ITokenProvider
    {
        public int AccessTokenLifetimeSeconds => 3600;
        public string CreateAccessToken(User user) => $"access-for-{user.Id}";
    }
}
