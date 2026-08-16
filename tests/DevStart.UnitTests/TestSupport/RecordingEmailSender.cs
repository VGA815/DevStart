using DevStart.Application.Abstractions.Authentication;

namespace DevStart.UnitTests.TestSupport
{
    internal sealed class RecordingEmailSender : IEmailSender
    {
        public List<(string Email, string Token)> Verifications { get; } = [];
        public List<(string Email, string Token)> PasswordResets { get; } = [];
        public List<(string Email, DateTime ExpiresAt)> SubscriptionExpirings { get; } = [];
        public List<(string Email, NewDeviceLoginInfo Info)> NewDeviceLogins { get; } = [];
        public List<(string Email, DateTime ScheduledFor)> AccountDeletionNotices { get; } = [];

        // When set, the corresponding send throws — used to simulate an SMTP outage and verify callers
        // remain enumeration-safe / don't surface a 500.
        public Exception? VerificationException { get; set; }
        public Exception? PasswordResetException { get; set; }

        public Task SendVerification(string email, string token)
        {
            Verifications.Add((email, token));
            if (VerificationException is not null)
            {
                throw VerificationException;
            }
            return Task.CompletedTask;
        }

        public Task SendPasswordReset(string email, string token)
        {
            PasswordResets.Add((email, token));
            if (PasswordResetException is not null)
            {
                throw PasswordResetException;
            }
            return Task.CompletedTask;
        }

        public Task SendSubscriptionExpiring(string email, DateTime expiresAt)
        {
            SubscriptionExpirings.Add((email, expiresAt));
            return Task.CompletedTask;
        }

        public Task SendNewDeviceLogin(string email, NewDeviceLoginInfo info)
        {
            NewDeviceLogins.Add((email, info));
            return Task.CompletedTask;
        }

        public Task SendAccountDeletionScheduled(string email, DateTime scheduledFor)
        {
            AccountDeletionNotices.Add((email, scheduledFor));
            return Task.CompletedTask;
        }
    }
}
