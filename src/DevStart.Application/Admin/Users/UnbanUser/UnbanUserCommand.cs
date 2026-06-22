using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Users.UnbanUser
{
    public sealed record UnbanUserCommand(Guid UserId, string? Reason) : ICommand;
}
