using DevStart.Infrastructure.AccountDeletion;
using DevStart.Infrastructure.Authentication;
using DevStart.Infrastructure.CommunityStandards;
using DevStart.Infrastructure.ExpertCollaborationRequests;
using DevStart.Infrastructure.Moderation;
using DevStart.Infrastructure.PatentRegistry;
using DevStart.Infrastructure.Payments;
using DevStart.Infrastructure.Subscriptions;
using DevStart.Infrastructure.Valuation;
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

            recurringJobManager.AddOrUpdate<ExpertCollaborationRequestExpiryJob>(
                "expert-collaboration-request-expiry",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily());

            recurringJobManager.AddOrUpdate<SessionCleanupJob>(
                "session-cleanup",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily());

            recurringJobManager.AddOrUpdate<AccountDeletionJob>(
                "account-deletion",
                job => job.RunAsync(CancellationToken.None),
                Cron.Daily());

            // Benchmark collection is quarterly: the derived multiple is a quarterly figure, so a more
            // frequent pull would add traffic and noise without adding information. Hangfire has no
            // Cron.Quarterly, hence the explicit "1st of January, April, July and October" expression.
            // Market caps first, revenue an hour later — both are independent, but staggering them keeps
            // two outbound bursts off the same minute.
            recurringJobManager.AddOrUpdate<MoexMarketCapCollectionJob>(
                "benchmark-marketcap-collection",
                job => job.RunAsync(CancellationToken.None),
                "0 3 1 1,4,7,10 *");

            recurringJobManager.AddOrUpdate<GirBoRevenueCollectionJob>(
                "benchmark-revenue-collection",
                job => job.RunAsync(CancellationToken.None),
                "0 4 1 1,4,7,10 *");

            // The Rospatent register refresh runs on the same quarterly rhythm as the benchmark
            // collectors, an hour after them so the three outbound bursts do not share a minute. The
            // register moves slowly and nothing downstream depends on same-day freshness: a record
            // resolves against whatever was last loaded, and a lapsed patent starts reading as lapsed
            // on the next refresh without a data migration.
            recurringJobManager.AddOrUpdate<PatentRegistryImportJob>(
                "patent-registry-import",
                job => job.RunAsync(CancellationToken.None),
                "0 5 1 1,4,7,10 *");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
