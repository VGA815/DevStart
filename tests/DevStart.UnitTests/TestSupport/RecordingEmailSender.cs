using DevStart.Application.Abstractions.Authentication;

namespace DevStart.UnitTests.TestSupport
{
    internal sealed class RecordingEmailSender : IEmailSender
    {
        public List<(string Email, string Token)> Verifications { get; } = [];
        public List<(string Email, string Token)> PasswordResets { get; } = [];
        public List<(string Email, DateTime ExpiresAt)> SubscriptionExpirings { get; } = [];

        public Task SendVerification(string email, string token)
        {
            Verifications.Add((email, token));
            return Task.CompletedTask;
        }

        public Task SendPasswordReset(string email, string token)
        {
            PasswordResets.Add((email, token));
            return Task.CompletedTask;
        }

        public Task SendSubscriptionExpiring(string email, DateTime expiresAt)
        {
            SubscriptionExpirings.Add((email, expiresAt));
            return Task.CompletedTask;
        }
    }
}
