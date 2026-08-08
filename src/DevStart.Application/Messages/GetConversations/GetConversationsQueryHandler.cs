using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Messages;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Messages.GetConversations
{
    internal sealed class GetConversationsQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetConversationsQuery, List<ConversationSummaryResponse>>
    {
        private const int MaxPageSize = 100;

        public async Task<Result<List<ConversationSummaryResponse>>> Handle(GetConversationsQuery query, CancellationToken cancellationToken)
        {
            // A page below 1 would produce a negative OFFSET, which the database rejects outright.
            int page = query.Page < 1 ? 1 : query.Page;
            int pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            Guid myId;
            ChatParticipantType mySide;

            if (query.AsStartupId.HasValue)
            {
                if (!await StartupIdentity.CanActAsAsync(context, query.AsStartupId.Value, userContext.UserId, cancellationToken))
                {
                    return Result.Failure<List<ConversationSummaryResponse>>(MessageErrors.StartupIdentityForbidden);
                }

                myId = query.AsStartupId.Value;
                mySide = ChatParticipantType.Startup;
            }
            else
            {
                myId = userContext.UserId;
                mySide = ChatParticipantType.User;
            }

            List<ConversationSummaryResponse> conversations = await context.Messages
                .AsNoTracking()
                .Where(m =>
                    (m.SenderType == mySide && m.SenderId == myId) ||
                    (m.ReceiverType == mySide && m.ReceiverId == myId))
                .GroupBy(m => new
                {
                    OtherType = m.SenderType == mySide && m.SenderId == myId ? m.ReceiverType : m.SenderType,
                    OtherId = m.SenderType == mySide && m.SenderId == myId ? m.ReceiverId : m.SenderId
                })
                .Select(g => new ConversationSummaryResponse
                {
                    OtherParticipantType = g.Key.OtherType,
                    OtherParticipantId = g.Key.OtherId,
                    UnreadCount = g.Count(m => !m.IsRead && m.ReceiverType == mySide && m.ReceiverId == myId),
                    LastMessageAt = g.Max(m => m.CreatedAt)
                })
                .OrderByDescending(c => c.LastMessageAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return conversations;
        }
    }
}
