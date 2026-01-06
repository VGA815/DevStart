using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Startups.Delete
{
    public sealed record DeleteStartupCommand(Guid StartupId) : ICommand;
}
