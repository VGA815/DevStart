using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ExternalLogins;

namespace DevStart.Application.Auth.OAuth.Start
{
    public sealed record StartOAuthCommand(
        ExternalLoginProvider Provider,
        string? RedirectUri,
        Guid? LinkUserId) : ICommand<StartOAuthResponse>;

    public sealed record StartOAuthResponse(string AuthorizationUrl, string State);
}
