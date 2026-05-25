using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.ResetPassword
{
    public sealed record ResetPasswordCommand(Guid TokenId, string NewPassword) : ICommand;
}
