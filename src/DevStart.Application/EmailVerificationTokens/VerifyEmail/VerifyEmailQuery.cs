using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.EmailVerificationTokens.VerifyEmail
{
    public sealed record VerifyEmailQuery(Guid TokenId) : IQuery<EmailVerificationResponse>;
}
