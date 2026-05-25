using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.ForgotPassword
{
    public sealed record ForgotPasswordCommand(string Email) : ICommand;
}
