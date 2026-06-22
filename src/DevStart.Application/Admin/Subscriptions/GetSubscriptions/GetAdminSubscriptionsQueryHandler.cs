using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Subscriptions;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Subscriptions.GetSubscriptions
{
    internal sealed class GetAdminSubscriptionsQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetAdminSubscriptionsQuery, List<AdminSubscriptionResponse>>
    {
        public async Task<Result<List<AdminSubscriptionResponse>>> Handle(
            GetAdminSubscriptionsQuery query,
            CancellationToken cancellationToken)
        {
            IQueryable<Subscription> subscriptions = context.Subscriptions.AsNoTracking();

            if (query.UserId.HasValue)
            {
                subscriptions = subscriptions.Where(s => s.UserId == query.UserId.Value);
            }
            if (query.Status.HasValue)
            {
                subscriptions = subscriptions.Where(s => s.Status == query.Status.Value);
            }
            if (query.Plan.HasValue)
            {
                subscriptions = subscriptions.Where(s => s.Plan == query.Plan.Value);
            }

            int pageSize = query.PageSize is > 0 and <= 200 ? query.PageSize : 50;
            int pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;

            // Left-join Users so the email is fetched in one pass (no correlated subquery / N+1).
            IQueryable<AdminSubscriptionResponse> projected =
                from s in subscriptions
                join u in context.Users on s.UserId equals u.Id into users
                from u in users.DefaultIfEmpty()
                select new AdminSubscriptionResponse
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    UserEmail = u != null ? u.Email : null,
                    Plan = s.Plan,
                    Status = s.Status,
                    Source = s.Source,
                    StartedAt = s.StartedAt,
                    ExpiresAt = s.ExpiresAt,
                    CreatedAt = s.CreatedAt,
                };

            List<AdminSubscriptionResponse> items = await projected
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return items;
        }
    }
}
