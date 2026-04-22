using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Notifications.MarkAllAsRead
{
    internal sealed class MarkAllNotificationsAsReadCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<MarkAllNotificationsAsReadCommand>
    {
        public async Task<Result> Handle(MarkAllNotificationsAsReadCommand command, CancellationToken cancellationToken)
        {
            await context.Notifications
                .Where(n => n.UserId == userContext.UserId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

            return Result.Success();
        }
    }
}
