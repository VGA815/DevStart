using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ExternalLogins;

namespace DevStart.Application.Auth.OAuth.Callback
{
    public sealed record HandleOAuthCallbackCommand(
        ExternalLoginProvider Provider,
        string Code,
        string State,
        string? IpAddress,
        string? UserAgent) : ICommand<OAuthAuthResult>;
}
