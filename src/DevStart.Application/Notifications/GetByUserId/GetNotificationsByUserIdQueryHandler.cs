using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Notifications.GetByUserId
{
    internal sealed class GetNotificationsByUserIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetNotificationsByUserIdQuery, List<NotificationResponse>>
    {
        public async Task<Result<List<NotificationResponse>>> Handle(GetNotificationsByUserIdQuery query, CancellationToken cancellationToken)
        {
            User? user = await context.Users.SingleOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);

            if (user == null)
            {
                return Result.Failure<List<NotificationResponse>>(UserErrors.NotFound(query.UserId));
            }
            if (user.Id != userContext.UserId)
            {
                return Result.Failure<List<NotificationResponse>>(UserErrors.Unauthorized());
            }

            List<NotificationResponse> notifications = await context.Notifications
                .Where(n => n.UserId == query.UserId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(n => new NotificationResponse
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Type = n.Type,
                    Title = n.Title,
                    Body = n.Body,
                    ReferenceId = n.ReferenceId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return notifications;
        }
    }
}
