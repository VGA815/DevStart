using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Notifications;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Notifications.MarkAsRead
{
    internal sealed class MarkNotificationAsReadCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<MarkNotificationAsReadCommand>
    {
        public async Task<Result> Handle(MarkNotificationAsReadCommand command, CancellationToken cancellationToken)
        {
            Notification? notification = await context.Notifications
                .SingleOrDefaultAsync(n => n.Id == command.NotificationId && n.UserId == userContext.UserId, cancellationToken);

            if (notification is null)
            {
                return Result.Failure(NotificationErrors.NotFound(command.NotificationId));
            }

            notification.MarkAsRead();
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
