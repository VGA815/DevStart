using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Messages.GetConversations
{
    internal sealed class GetConversationsQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetConversationsQuery, List<ConversationSummaryResponse>>
    {
        public async Task<Result<List<ConversationSummaryResponse>>> Handle(GetConversationsQuery query, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            List<ConversationSummaryResponse> conversations = await context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g => new ConversationSummaryResponse
                {
                    OtherUserId = g.Key,
                    UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead),
                    LastMessageAt = g.Max(m => m.CreatedAt)
                })
                .OrderByDescending(c => c.LastMessageAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return conversations;
        }
    }
}
