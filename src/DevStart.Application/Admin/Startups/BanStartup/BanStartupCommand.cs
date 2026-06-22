using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Startups.BanStartup
{
    public sealed record BanStartupCommand(
        Guid StartupId,
        string Reason,
        DateTime? ExpiresAt) : ICommand;
}
