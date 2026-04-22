using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Notifications.GetById
{
    internal sealed class GetNotificationByIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetNotificationByIdQuery, NotificationResponse>
    {
        public async Task<Result<NotificationResponse>> Handle(GetNotificationByIdQuery query, CancellationToken cancellationToken)
        {
            NotificationResponse? notification = await context.Notifications
                .AsNoTracking()
                .Where(n => n.Id == query.NotificationId && n.UserId == userContext.UserId)
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
                .SingleOrDefaultAsync(cancellationToken);

            if (notification is null)
            {
                return Result.Failure<NotificationResponse>(NotificationErrors.NotFound(query.NotificationId));
            }

            return notification;
        }
    }
}
