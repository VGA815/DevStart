using DevStart.Domain.Users;

namespace DevStart.Application.Abstractions.Authentication
{
    public interface ITokenProvider
    {
        string CreateAccessToken(User user);

        int AccessTokenLifetimeSeconds { get; }
    }
}
