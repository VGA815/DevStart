using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupFollowers.Delete
{
    public sealed record DeleteStartupFollowerCommand(Guid StartupId) : ICommand;
}
