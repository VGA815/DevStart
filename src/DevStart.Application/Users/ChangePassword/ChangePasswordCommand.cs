using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.ChangePassword
{
    public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand;
}
