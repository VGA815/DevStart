using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.Login
{
    public sealed record LoginUserCommand(
        string Email,
        string Password,
        string? IpAddress,
        string? UserAgent) : ICommand<TokenPair>;
}
