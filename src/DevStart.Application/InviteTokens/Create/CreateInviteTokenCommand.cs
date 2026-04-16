using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InviteTokens.Create
{
    public sealed record CreateInviteTokenCommand(Guid StartupId) : ICommand<Guid>;
}
