using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Notifications.GetUnreadCount
{
    internal sealed class GetUnreadCountQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetUnreadCountQuery, int>
    {
        public async Task<Result<int>> Handle(GetUnreadCountQuery query, CancellationToken cancellationToken)
        {
            int count = await context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userContext.UserId && !n.IsRead, cancellationToken);

            return count;
        }
    }
}
