using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Admin;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevStart.Infrastructure.Moderation
{
    /// <summary>
    /// Recurring Hangfire job that lifts temporary bans whose <c>BanExpiresAt</c> has passed, for both
    /// users and startups. User unbans drop the cached user projections via the domain event handler;
    /// startup unbans drop the startup cache here. Each auto-unban is recorded in the admin audit log
    /// with a null admin id (system action).
    /// </summary>
    public sealed class BanExpiryJob(
        IApplicationDbContext context,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider,
        ILogger<BanExpiryJob> logger)
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;

            List<User> users = await context.Users
                .Where(u => u.IsBanned && u.BanExpiresAt != null && u.BanExpiresAt <= now)
                .ToListAsync(cancellationToken);
            int unbannedUsers = 0;
            foreach (User user in users)
            {
                Result unban = user.Unban(now);
                if (unban.IsFailure)
                {
                    logger.LogWarning(
                        "Auto-unban skipped for user {UserId}: {Error}", user.Id, unban.Error.Code);
                    continue;
                }
                unbannedUsers++;
                context.AdminActionLogs.Add(AdminActionLog.Create(
                    adminUserId: null,
                    AdminActionType.UnbanUser,
                    AdminTargetType.User,
                    user.Id,
                    "Automatic unban: temporary ban expired",
                    now));
            }

            List<Startup> startups = await context.Startups
                .Where(s => s.IsBanned && s.BanExpiresAt != null && s.BanExpiresAt <= now)
                .ToListAsync(cancellationToken);
            var unbannedStartups = new List<Startup>();
            foreach (Startup startup in startups)
            {
                Result unban = startup.Unban(now);
                if (unban.IsFailure)
                {
                    logger.LogWarning(
                        "Auto-unban skipped for startup {StartupId}: {Error}", startup.Id, unban.Error.Code);
                    continue;
                }
                unbannedStartups.Add(startup);
                context.AdminActionLogs.Add(AdminActionLog.Create(
                    adminUserId: null,
                    AdminActionType.UnbanStartup,
                    AdminTargetType.Startup,
                    startup.Id,
                    "Automatic unban: temporary ban expired",
                    now));
            }

            if (unbannedUsers == 0 && unbannedStartups.Count == 0)
            {
                return;
            }

            await context.SaveChangesAsync(cancellationToken);

            // User caches are dropped by the UserUnbanned domain event handler; startups have no event.
            foreach (Startup startup in unbannedStartups)
            {
                await cacheService.RemoveAsync(CacheKeys.Startup(startup.Id), cancellationToken);
            }

            logger.LogInformation(
                "Ban-expiry job lifted {UserCount} user ban(s) and {StartupCount} startup ban(s).",
                unbannedUsers, unbannedStartups.Count);
        }
    }
}
