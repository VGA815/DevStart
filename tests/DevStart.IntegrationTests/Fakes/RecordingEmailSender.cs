using System.Collections.Concurrent;
using DevStart.Application.Abstractions.Authentication;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>In-memory <see cref="IEmailSender"/> that records what would have been sent so tests can
    /// assert that, e.g., a verification email was triggered without standing up SMTP.</summary>
    internal sealed class RecordingEmailSender : IEmailSender
    {
        public ConcurrentQueue<(string Email, string Token)> Verifications { get; } = new();
        public ConcurrentQueue<(string Email, string Token)> PasswordResets { get; } = new();
        public ConcurrentQueue<(string Email, DateTime ExpiresAt)> ExpiringReminders { get; } = new();
        public ConcurrentQueue<(string Email, NewDeviceLoginInfo Info)> NewDeviceLogins { get; } = new();
        public ConcurrentQueue<(string Email, DateTime ScheduledFor)> AccountDeletionNotices { get; } = new();

        public Task SendVerification(string email, string token)
        {
            Verifications.Enqueue((email, token));
            return Task.CompletedTask;
        }

        public Task SendPasswordReset(string email, string token)
        {
            PasswordResets.Enqueue((email, token));
            return Task.CompletedTask;
        }

        public Task SendSubscriptionExpiring(string email, DateTime expiresAt)
        {
            ExpiringReminders.Enqueue((email, expiresAt));
            return Task.CompletedTask;
        }

        public Task SendNewDeviceLogin(string email, NewDeviceLoginInfo info)
        {
            NewDeviceLogins.Enqueue((email, info));
            return Task.CompletedTask;
        }

        public Task SendAccountDeletionScheduled(string email, DateTime scheduledFor)
        {
            AccountDeletionNotices.Enqueue((email, scheduledFor));
            return Task.CompletedTask;
        }
    }
}
