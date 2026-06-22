using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Startups.UnbanStartup
{
    internal sealed class UnbanStartupCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UnbanStartupCommand>
    {
        public async Task<Result> Handle(UnbanStartupCommand command, CancellationToken cancellationToken)
        {
            Startup? startup = await context.Startups
                .SingleOrDefaultAsync(s => s.Id == command.StartupId, cancellationToken);
            if (startup is null)
            {
                return Result.Failure(StartupErrors.NotFound(command.StartupId));
            }

            DateTime now = dateTimeProvider.UtcNow;
            Result unban = startup.Unban(now);
            if (unban.IsFailure)
            {
                return unban;
            }

            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.UnbanStartup,
                AdminTargetType.Startup,
                startup.Id,
                command.Reason ?? "Unbanned",
                now));

            await context.SaveChangesAsync(cancellationToken);
            await cacheService.RemoveAsync(CacheKeys.Startup(startup.Id), cancellationToken);

            return Result.Success();
        }
    }
}
