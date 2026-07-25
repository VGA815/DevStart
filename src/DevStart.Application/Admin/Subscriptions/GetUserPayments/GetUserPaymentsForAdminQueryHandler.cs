using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Subscriptions.GetUserPayments
{
    internal sealed class GetUserPaymentsForAdminQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetUserPaymentsForAdminQuery, List<AdminPaymentResponse>>
    {
        public async Task<Result<List<AdminPaymentResponse>>> Handle(
            GetUserPaymentsForAdminQuery query,
            CancellationToken cancellationToken)
        {
            List<AdminPaymentResponse> items = await context.Payments
                .AsNoTracking()
                .Where(p => p.UserId == query.UserId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new AdminPaymentResponse
                {
                    Id = p.Id,
                    SubscriptionId = p.SubscriptionId,
                    ServiceOrderId = p.ServiceOrderId,
                    Purpose = p.Purpose,
                    Amount = p.Amount,
                    DiscountAmount = p.DiscountAmount,
                    RefundedAmount = p.RefundedAmount,
                    Currency = p.Currency,
                    Status = p.Status,
                    PromoCodeId = p.PromoCodeId,
                    CreatedAt = p.CreatedAt,
                    PaidAt = p.PaidAt,
                })
                .ToListAsync(cancellationToken);

            return items;
        }
    }
}
