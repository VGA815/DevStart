using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupMembers.Delete
{
    public sealed record DeleteStartupMemberCommand(Guid StartupId, Guid ProfileId) : ICommand;
}
