using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Notifications.GetByUserId
{
    internal sealed class GetNotificationsByUserIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetNotificationsByUserIdQuery, List<NotificationResponse>>
    {
        public async Task<Result<List<NotificationResponse>>> Handle(GetNotificationsByUserIdQuery query, CancellationToken cancellationToken)
        {
            IQueryable<Notification> notifications = context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userContext.UserId);

            if (query.IsRead.HasValue)
            {
                notifications = notifications.Where(n => n.IsRead == query.IsRead.Value);
            }

            List<NotificationResponse> items = await notifications
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

            return items;
        }
    }
}
