using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Auth.RefreshToken
{
    public sealed record RefreshTokenCommand(
        string RefreshToken,
        string? IpAddress,
        string? UserAgent) : ICommand<TokenPair>;
}
