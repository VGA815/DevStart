using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.ServiceOrders;
using DevStart.Domain.Admin;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.ServiceOrders.CancelServiceOrder
{
    internal sealed class CancelServiceOrderCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ICacheService cacheService,
        IServiceEntitlementChecker entitlementChecker,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CancelServiceOrderCommand>
    {
        public async Task<Result> Handle(CancelServiceOrderCommand command, CancellationToken cancellationToken)
        {
            ServiceOrder? order = await context.ServiceOrders
                .SingleOrDefaultAsync(o => o.Id == command.ServiceOrderId, cancellationToken);
            if (order is null)
            {
                return Result.Failure(ServiceOrderErrors.NotFound(command.ServiceOrderId));
            }

            DateTime now = dateTimeProvider.UtcNow;

            // A refunded order is already closed and its money returned; cancelling it on top would
            // overwrite that outcome in the record.
            Result cancelled = order.MarkCancelled(now);
            if (cancelled.IsFailure)
            {
                return cancelled;
            }

            if (order.ServiceType == ServiceType.Promotion)
            {
                Startup? startup = await context.Startups
                    .SingleOrDefaultAsync(s => s.Id == order.TargetId, cancellationToken);
                if (startup is not null)
                {
                    startup.ClearFeature(now);
                    await cacheService.RemoveAsync(CacheKeys.Startup(startup.Id), cancellationToken);
                }
            }

            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.CancelServiceOrder,
                AdminTargetType.ServiceOrder,
                order.Id,
                command.Reason,
                now));

            await context.SaveChangesAsync(cancellationToken);

            // Revoke the entitlement immediately rather than waiting out the cached answer.
            await entitlementChecker.InvalidateAsync(order.UserId, cancellationToken);

            return Result.Success();
        }
    }
}
