using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.OAuth;

namespace DevStart.Application.Auth.TwoFactor.VerifyLogin
{
    public sealed record VerifyTwoFactorLoginCommand(
        string PendingToken,
        string Code,
        string? IpAddress,
        string? UserAgent,
        bool RememberDevice = false) : ICommand<OAuthAuthResult>;
}
