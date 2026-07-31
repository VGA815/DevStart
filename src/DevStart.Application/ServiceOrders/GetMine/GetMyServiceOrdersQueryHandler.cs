using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ServiceOrders;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ServiceOrders.GetMine
{
    internal sealed class GetMyServiceOrdersQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetMyServiceOrdersQuery, List<ServiceOrderResponse>>
    {
        public async Task<Result<List<ServiceOrderResponse>>> Handle(
            GetMyServiceOrdersQuery query,
            CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;
            DateTime utcNow = dateTimeProvider.UtcNow;

            List<ServiceOrder> orders = await context.ServiceOrders
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(cancellationToken);

            // Only startup-targeted services have a name worth showing; a deal is identified by its id.
            List<Guid> startupIds = orders
                .Where(o => ServiceTargets.KindOf(o.ServiceType) == ServiceTargetKind.Startup
                         && o.TargetId.HasValue)
                .Select(o => o.TargetId!.Value)
                .Distinct()
                .ToList();

            Dictionary<Guid, string> startupNames = await context.Startups
                .AsNoTracking()
                .Where(s => startupIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

            return orders
                .Select(o => new ServiceOrderResponse
                {
                    Id = o.Id,
                    ServiceType = o.ServiceType,
                    TargetKind = ServiceTargets.KindOf(o.ServiceType),
                    TargetId = o.TargetId,
                    TargetName = o.TargetId is Guid tid && startupNames.TryGetValue(tid, out string? name)
                        ? name
                        : null,
                    Amount = o.Amount,
                    Currency = o.Currency,
                    Status = o.Status,
                    IsActive = o.GrantsAccess(utcNow),
                    CreatedAt = o.CreatedAt,
                    PaidAt = o.PaidAt,
                    FulfilledAt = o.FulfilledAt,
                    ExpiresAt = o.ExpiresAt,
                })
                .ToList();
        }
    }
}
