using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.OAuth;

namespace DevStart.Application.Users.Login
{
    public sealed record LoginUserCommand(
        string Email,
        string Password,
        string? IpAddress,
        string? UserAgent) : ICommand<OAuthAuthResult>;
}
