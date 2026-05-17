using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ExternalLogins;

namespace DevStart.Application.Auth.OAuth.Unlink
{
    public sealed record UnlinkExternalLoginCommand(
        Guid UserId,
        ExternalLoginProvider Provider) : ICommand;
}
