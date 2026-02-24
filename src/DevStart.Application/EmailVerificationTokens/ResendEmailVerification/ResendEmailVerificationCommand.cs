using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.EmailVerificationTokens.ResendEmailVerification
{
    public sealed record ResendEmailVerificationCommand(string Email) : ICommand;
}
