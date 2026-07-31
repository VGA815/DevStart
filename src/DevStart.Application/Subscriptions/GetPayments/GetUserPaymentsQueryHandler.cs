using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Payments;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Subscriptions.GetPayments
{
    internal sealed class GetUserPaymentsQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : IQueryHandler<GetUserPaymentsQuery, List<PaymentHistoryResponse>>
    {
        public async Task<Result<List<PaymentHistoryResponse>>> Handle(
            GetUserPaymentsQuery query,
            CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            List<Payment> payments = await context.Payments
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            List<Guid> subscriptionIds = payments
                .Where(p => p.SubscriptionId.HasValue)
                .Select(p => p.SubscriptionId!.Value)
                .Distinct()
                .ToList();

            Dictionary<Guid, SubscriptionPlan> plans = await context.Subscriptions
                .AsNoTracking()
                .Where(s => subscriptionIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Plan, cancellationToken);

            // A service-order payment has no plan, so the row is labelled by the service it bought
            // instead of being defaulted to Pro (which read as a subscription the user never had).
            List<Guid> serviceOrderIds = payments
                .Where(p => p.ServiceOrderId.HasValue)
                .Select(p => p.ServiceOrderId!.Value)
                .Distinct()
                .ToList();

            Dictionary<Guid, ServiceType> services = await context.ServiceOrders
                .AsNoTracking()
                .Where(o => serviceOrderIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.ServiceType, cancellationToken);

            List<PaymentHistoryResponse> items = payments
                .Select(p => new PaymentHistoryResponse
                {
                    Id = p.Id,
                    SubscriptionId = p.SubscriptionId,
                    ServiceOrderId = p.ServiceOrderId,
                    Purpose = p.Purpose,
                    Plan = p.SubscriptionId is Guid sid && plans.TryGetValue(sid, out SubscriptionPlan plan)
                        ? plan
                        : null,
                    ServiceType = p.ServiceOrderId is Guid oid && services.TryGetValue(oid, out ServiceType service)
                        ? service
                        : null,
                    Amount = p.Amount,
                    RefundedAmount = p.RefundedAmount,
                    Currency = p.Currency,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    PaidAt = p.PaidAt,
                })
                .ToList();

            return items;
        }
    }
}
