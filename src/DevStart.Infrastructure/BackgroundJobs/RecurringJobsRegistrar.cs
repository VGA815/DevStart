using DevStart.Infrastructure.CommunityStandards;
using DevStart.Infrastructure.Moderation;
using DevStart.Infrastructure.Payments;
using DevStart.Infrastructure.Subscriptions;
using Hangfire;
using Microsoft.Extensions.Hosting;

namespace DevStart.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Registers the recurring billing jobs with Hangfire at startup. Using AddOrUpdate keeps the
    /// schedules idempotent across deployments.
    /// </summary>
    internal sealed class RecurringJobsRegistrar(IRecurringJobManager recurringJobManager) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            recurringJobManager.AddOrUpdate<PaymentReconciliationJob>(
                "payments-reconciliation",
                job => job.ReconcilePendingAsync(CancellationToken.None),
                "*/15 * * * *");

            recurringJobManager.AddOrUpdate<SubscriptionMaintenanceJob>(
                "subscription-maintenance",
                job => job.RunAsync(CancellationToken.None),
                Cron.Hourly());

            recurringJobManager.AddOrUpdate<BanExpiryJob>(
                "ban-expiry",
                job => job.RunAsync(CancellationToken.None),
                Cron.Hourly());

            recurringJobManager.AddOrUpdate<CommunityStandardsRefreshJob>(
                "community-standards-refresh",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily());

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
