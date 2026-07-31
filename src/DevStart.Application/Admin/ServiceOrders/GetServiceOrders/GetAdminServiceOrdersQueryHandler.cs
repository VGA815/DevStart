using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ServiceOrders;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.ServiceOrders.GetServiceOrders
{
    internal sealed class GetAdminServiceOrdersQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetAdminServiceOrdersQuery, List<AdminServiceOrderResponse>>
    {
        public async Task<Result<List<AdminServiceOrderResponse>>> Handle(
            GetAdminServiceOrdersQuery query,
            CancellationToken cancellationToken)
        {
            IQueryable<ServiceOrder> orders = context.ServiceOrders.AsNoTracking();

            if (query.UserId.HasValue)
            {
                orders = orders.Where(o => o.UserId == query.UserId.Value);
            }
            if (query.Status.HasValue)
            {
                orders = orders.Where(o => o.Status == query.Status.Value);
            }
            if (query.ServiceType.HasValue)
            {
                orders = orders.Where(o => o.ServiceType == query.ServiceType.Value);
            }

            int pageSize = query.PageSize is > 0 and <= 200 ? query.PageSize : 50;
            int pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;
            DateTime utcNow = dateTimeProvider.UtcNow;

            // Left-join Users so the email is fetched in one pass (no correlated subquery / N+1).
            var projected =
                from o in orders
                join u in context.Users on o.UserId equals u.Id into users
                from u in users.DefaultIfEmpty()
                select new
                {
                    Order = o,
                    UserEmail = u != null ? u.Email : null,
                };

            var rows = await projected
                .OrderByDescending(r => r.Order.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return rows
                .Select(r => new AdminServiceOrderResponse
                {
                    Id = r.Order.Id,
                    UserId = r.Order.UserId,
                    UserEmail = r.UserEmail,
                    ServiceType = r.Order.ServiceType,
                    TargetKind = ServiceTargets.KindOf(r.Order.ServiceType),
                    TargetId = r.Order.TargetId,
                    Amount = r.Order.Amount,
                    Currency = r.Order.Currency,
                    Status = r.Order.Status,
                    IsActive = r.Order.GrantsAccess(utcNow),
                    CreatedAt = r.Order.CreatedAt,
                    PaidAt = r.Order.PaidAt,
                    FulfilledAt = r.Order.FulfilledAt,
                    ExpiresAt = r.Order.ExpiresAt,
                    CancelledAt = r.Order.CancelledAt,
                    RefundedAt = r.Order.RefundedAt,
                })
                .ToList();
        }
    }
}
