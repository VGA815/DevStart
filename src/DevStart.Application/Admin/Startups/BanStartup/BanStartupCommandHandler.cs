using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DevStart.Application.Admin.Startups.BanStartup
{
    internal sealed class BanStartupCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<BanStartupCommand>
    {
        public async Task<Result> Handle(BanStartupCommand command, CancellationToken cancellationToken)
        {
            Startup? startup = await context.Startups
                .SingleOrDefaultAsync(s => s.Id == command.StartupId, cancellationToken);
            if (startup is null)
            {
                return Result.Failure(StartupErrors.NotFound(command.StartupId));
            }

            DateTime now = dateTimeProvider.UtcNow;
            Result ban = startup.Ban(command.Reason, command.ExpiresAt, userContext.UserId, now);
            if (ban.IsFailure)
            {
                return ban;
            }

            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.BanStartup,
                AdminTargetType.Startup,
                startup.Id,
                command.Reason,
                now,
                JsonSerializer.Serialize(new { expiresAt = command.ExpiresAt })));

            await context.SaveChangesAsync(cancellationToken);
            await cacheService.RemoveAsync(CacheKeys.Startup(startup.Id), cancellationToken);

            return Result.Success();
        }
    }
}
