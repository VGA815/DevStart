using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Users.GetUserDetail
{
    internal sealed class GetUserDetailQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetUserDetailQuery, AdminUserDetailResponse>
    {
        public async Task<Result<AdminUserDetailResponse>> Handle(
            GetUserDetailQuery query,
            CancellationToken cancellationToken)
        {
            User? user = await context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<AdminUserDetailResponse>(UserErrors.NotFound(query.UserId));
            }

            // Most relevant subscription: the active one if any, otherwise the latest.
            AdminUserSubscriptionSummary? subscription = await context.Subscriptions
                .AsNoTracking()
                .Where(s => s.UserId == query.UserId)
                .OrderByDescending(s => s.Status == SubscriptionStatus.Active)
                .ThenByDescending(s => s.CreatedAt)
                .Select(s => new AdminUserSubscriptionSummary
                {
                    Id = s.Id,
                    Plan = s.Plan,
                    Status = s.Status,
                    Source = s.Source,
                    StartedAt = s.StartedAt,
                    ExpiresAt = s.ExpiresAt,
                })
                .FirstOrDefaultAsync(cancellationToken);

            return new AdminUserDetailResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsVerified = user.IsVerified,
                IsBanned = user.IsBanned,
                BanReason = user.BanReason,
                BannedAt = user.BannedAt,
                BanExpiresAt = user.BanExpiresAt,
                BannedByUserId = user.BannedByUserId,
                CreatedAt = user.CreatedAt,
                CurrentSubscription = subscription,
            };
        }
    }
}
