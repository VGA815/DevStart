using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Users.ResetTwoFactor
{
    public sealed record ResetUserTwoFactorCommand(Guid UserId, string Reason) : ICommand;
}
