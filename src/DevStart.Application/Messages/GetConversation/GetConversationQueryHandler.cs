using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Messages.GetById;
using DevStart.Domain.Messages;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Messages.GetConversation
{
    internal sealed class GetConversationQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetConversationQuery, List<MessageResponse>>
    {
        private const int MaxPageSize = 100;

        public async Task<Result<List<MessageResponse>>> Handle(GetConversationQuery query, CancellationToken cancellationToken)
        {
            Guid myId;
            ChatParticipantType mySide;

            if (query.AsStartupId.HasValue)
            {
                bool isMember = await context.StartupMembers.AnyAsync(
                    sm => sm.StartupId == query.AsStartupId.Value && sm.ProfileId == userContext.UserId,
                    cancellationToken);

                if (!isMember)
                {
                    return Result.Failure<List<MessageResponse>>(MessageErrors.Unauthorized);
                }

                myId = query.AsStartupId.Value;
                mySide = ChatParticipantType.Startup;
            }
            else
            {
                myId = userContext.UserId;
                mySide = ChatParticipantType.User;
            }

            ChatParticipantType otherType = query.OtherType;
            Guid otherId = query.OtherId;

            // A page below 1 would produce a negative OFFSET, which the database rejects outright.
            int page = query.Page < 1 ? 1 : query.Page;
            int pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            List<MessageResponse> messages = await context.Messages
                .AsNoTracking()
                .Where(m =>
                    (m.SenderType == mySide && m.SenderId == myId && m.ReceiverType == otherType && m.ReceiverId == otherId) ||
                    (m.SenderType == otherType && m.SenderId == otherId && m.ReceiverType == mySide && m.ReceiverId == myId))
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MessageResponse
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderType = m.SenderType,
                    ReceiverId = m.ReceiverId,
                    ReceiverType = m.ReceiverType,
                    TextContent = m.TextContent,
                    MediaIds = m.MediaIds,
                    MetricIds = m.MetricIds,
                    DocumentIds = m.DocumentIds,
                    FileIds = m.FileIds,
                    IsRead = m.IsRead,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return messages;
        }
    }
}
