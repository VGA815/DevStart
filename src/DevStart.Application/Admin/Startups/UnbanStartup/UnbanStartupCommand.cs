using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Startups.UnbanStartup
{
    public sealed record UnbanStartupCommand(Guid StartupId, string? Reason) : ICommand;
}
