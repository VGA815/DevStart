using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Messages.GetConversation
{
    internal sealed class GetConversationQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetConversationQuery, List<MessageResponse>>
    {
        public async Task<Result<List<MessageResponse>>> Handle(GetConversationQuery query, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            List<MessageResponse> messages = await context.Messages
                .AsNoTracking()
                .Where(m => (m.SenderId == userId && m.ReceiverId == query.OtherUserId) ||
                            (m.SenderId == query.OtherUserId && m.ReceiverId == userId))
                .OrderByDescending(m => m.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(m => new MessageResponse
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    TextContent = m.TextContent,
                    MediaIds = m.MediaIds,
                    MetricIds = m.MetricIds,
                    IsRead = m.IsRead,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return messages;
        }
    }
}
