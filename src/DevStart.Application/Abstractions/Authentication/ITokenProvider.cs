using DevStart.Domain.Users;

namespace DevStart.Application.Abstractions.Authentication
{
    public interface ITokenProvider
    {
        string Create(User user);
    }
}
