using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InviteTokens.Use
{
    public sealed record UseInviteTokenCommand(Guid TokenId) : ICommand<Guid>;
}
