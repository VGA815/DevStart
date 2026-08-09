using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Application.CommunityStandards;
using DevStart.Domain.Notifications;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DevStart.Infrastructure.CommunityStandards
{
    /// <summary>
    /// Nightly sweep that keeps the catalog projection honest. Document and profile edits refresh the
    /// projection immediately; this job catches the checklist items nothing else notifies on — team
    /// size, roadmap items and the pitch deck. It then nudges founders whose checklist is still short.
    /// </summary>
    public sealed class CommunityStandardsRefreshJob(
        IServiceScopeFactory scopeFactory,
        IDateTimeProvider dateTimeProvider,
        ILogger<CommunityStandardsRefreshJob> logger)
    {
        /// <summary>
        /// Startups refreshed per scope. Each batch gets a fresh DbContext: the refresher tracks the
        /// projection row it upserts, so reusing one context across the whole catalog would make every
        /// subsequent SaveChanges walk an ever-larger change tracker. Kept sequential deliberately —
        /// a nightly job does not need parallelism, and concurrent scopes would only add connection-pool
        /// pressure.
        /// </summary>
        private const int BatchSize = 200;

        /// <summary>Grace period before the first nudge — a startup created today is expected to be incomplete.</summary>
        private static readonly TimeSpan NewStartupGrace = TimeSpan.FromDays(7);

        /// <summary>How long a nudge suppresses the next one for the same startup.</summary>
        private static readonly TimeSpan ReminderInterval = TimeSpan.FromDays(30);

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;

            List<Guid> startupIds = await LoadStartupIdsAsync(now, cancellationToken);

            foreach (Guid[] batch in startupIds.Chunk(BatchSize))
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                ICommunityStandardsRefresher refresher =
                    scope.ServiceProvider.GetRequiredService<ICommunityStandardsRefresher>();

                foreach (Guid startupId in batch)
                {
                    await refresher.RefreshAsync(startupId, cancellationToken);
                }
            }

            int notified = await NotifyIncompleteFoundersAsync(now, cancellationToken);

            logger.LogInformation(
                "Community-standards refresh covered {StartupCount} startup(s) and sent {NotificationCount} reminder(s).",
                startupIds.Count, notified);
        }

        private async Task<List<Guid>> LoadStartupIdsAsync(DateTime now, CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            IApplicationDbContext context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            // Banned startups are invisible in the catalog, so their badge is moot.
            return await context.Startups
                .AsNoTracking()
                .Where(s => !(s.IsBanned && (s.BanExpiresAt == null || s.BanExpiresAt > now)))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);
        }

        private async Task<int> NotifyIncompleteFoundersAsync(DateTime now, CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            IApplicationDbContext context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            INotificationService notificationService =
                scope.ServiceProvider.GetRequiredService<INotificationService>();

            DateTime createdBefore = now - NewStartupGrace;
            DateTime remindedSince = now - ReminderInterval;

            var candidates = await context.StartupCommunityStandards
                .AsNoTracking()
                .Where(cs => cs.Level != CommunityStandardsLevel.Complete)
                .Join(
                    context.Startups.AsNoTracking().Where(s => s.CreatedAt <= createdBefore),
                    cs => cs.StartupId,
                    s => s.Id,
                    (cs, s) => new { s.Id, s.Name, cs.CompletedCount, cs.TotalCount })
                .ToListAsync(cancellationToken);

            if (candidates.Count == 0)
            {
                return 0;
            }

            List<Guid> candidateIds = candidates.Select(c => c.Id).ToList();

            // One nudge per startup per interval, deduped against the notifications already sent rather
            // than against extra state of our own.
            HashSet<Guid> recentlyReminded = (await context.Notifications
                .AsNoTracking()
                .Where(n => n.Type == NotificationType.CommunityStandardsIncomplete
                         && n.ReferenceId != null
                         && candidateIds.Contains(n.ReferenceId.Value)
                         && n.CreatedAt >= remindedSince)
                .Select(n => n.ReferenceId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();

            List<Guid> targets = candidateIds.Where(id => !recentlyReminded.Contains(id)).ToList();
            if (targets.Count == 0)
            {
                return 0;
            }

            List<StartupFounder> founders = await context.StartupMembers
                .AsNoTracking()
                .Where(sm => targets.Contains(sm.StartupId) && sm.Role == StartupRole.Founder)
                .Select(sm => new StartupFounder(sm.StartupId, sm.ProfileId))
                .ToListAsync(cancellationToken);

            Dictionary<Guid, (string Name, int Completed, int Total)> byStartup = candidates
                .ToDictionary(c => c.Id, c => (c.Name, c.CompletedCount, c.TotalCount));

            var notifications = new List<Notification>(founders.Count);

            foreach (StartupFounder founder in founders)
            {
                if (!byStartup.TryGetValue(founder.StartupId, out (string Name, int Completed, int Total) info))
                {
                    continue;
                }

                notifications.Add(Notification.Create(
                    userId: founder.ProfileId,
                    type: NotificationType.CommunityStandardsIncomplete,
                    title: "Стандарты сообщества не заполнены",
                    body: $"У стартапа «{info.Name}» выполнено {info.Completed} из {info.Total} пунктов "
                        + "чек-листа стандартов сообщества. Заполните недостающие, чтобы инвесторам и "
                        + "экспертам было проще вам довериться.",
                    createdAt: now,
                    referenceId: founder.StartupId));
            }

            await notificationService.PublishManyAsync(notifications, cancellationToken);

            return notifications.Count;
        }

        private readonly record struct StartupFounder(Guid StartupId, Guid ProfileId);
    }
}
