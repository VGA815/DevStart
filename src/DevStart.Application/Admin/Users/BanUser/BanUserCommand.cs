using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Users.BanUser
{
    public sealed record BanUserCommand(
        Guid UserId,
        string Reason,
        DateTime? ExpiresAt) : ICommand;
}
