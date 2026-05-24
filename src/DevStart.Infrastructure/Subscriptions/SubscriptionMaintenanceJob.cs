using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Domain.Notifications;
using DevStart.Domain.Subscriptions;
using DevStart.Infrastructure.Payments;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevStart.Infrastructure.Subscriptions
{
    /// <summary>
    /// Recurring Hangfire job that (1) sends a one-time renewal reminder before a Pro subscription
    /// expires and (2) transitions subscriptions whose term has ended to
    /// <see cref="SubscriptionStatus.Expired"/>, dropping the active-pro cache so access is revoked
    /// immediately. Idempotent: reminders are de-duplicated via <c>RenewalReminderSentAt</c>.
    /// </summary>
    public sealed class SubscriptionMaintenanceJob(
        IApplicationDbContext context,
        INotificationService notificationService,
        IEmailSender emailSender,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider,
        IOptions<BillingMaintenanceOptions> options,
        ILogger<SubscriptionMaintenanceJob> logger)
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;
            DateTime reminderThreshold = now.AddDays(options.Value.ReminderDaysBefore);

            await SendRenewalRemindersAsync(now, reminderThreshold, cancellationToken);
            await ExpireEndedSubscriptionsAsync(now, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
        }

        private async Task SendRenewalRemindersAsync(
            DateTime now, DateTime reminderThreshold, CancellationToken cancellationToken)
        {
            List<Subscription> expiringSoon = await context.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active
                         && s.ExpiresAt > now
                         && s.ExpiresAt <= reminderThreshold
                         && s.RenewalReminderSentAt == null)
                .ToListAsync(cancellationToken);

            foreach (Subscription subscription in expiringSoon)
            {
                await notificationService.PublishAsync(Notification.Create(
                    userId: subscription.UserId,
                    type: NotificationType.SubscriptionExpiringSoon,
                    title: "Подписка скоро истечёт",
                    body: $"Ваша подписка Pro истекает {subscription.ExpiresAt:yyyy-MM-dd}. Продлите доступ.",
                    createdAt: now,
                    referenceId: subscription.Id), cancellationToken);

                string? email = await context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == subscription.UserId)
                    .Select(u => u.Email)
                    .SingleOrDefaultAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(email))
                {
                    try
                    {
                        await emailSender.SendSubscriptionExpiring(email, subscription.ExpiresAt);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Failed to send renewal reminder email for subscription {SubscriptionId}.",
                            subscription.Id);
                    }
                }

                subscription.MarkRenewalReminderSent(now);
            }
        }

        private async Task ExpireEndedSubscriptionsAsync(DateTime now, CancellationToken cancellationToken)
        {
            List<Subscription> ended = await context.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && s.ExpiresAt <= now)
                .ToListAsync(cancellationToken);

            foreach (Subscription subscription in ended)
            {
                subscription.MarkExpired(now);
                await cacheService.RemoveAsync(
                    CacheKeys.SubscriptionActiveByUser(subscription.UserId), cancellationToken);
                await notificationService.PublishAsync(Notification.Create(
                    userId: subscription.UserId,
                    type: NotificationType.SubscriptionExpired,
                    title: "Подписка истекла",
                    body: "Ваша подписка Pro истекла. Оформите новую, чтобы вернуть доступ.",
                    createdAt: now,
                    referenceId: subscription.Id), cancellationToken);
            }
        }
    }
}
