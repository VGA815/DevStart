using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Messages;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Messages.GetById
{
    internal sealed class GetMessageByIdQueryHandler(IApplicationDbContext context, IUserContext userContext)
        : IQueryHandler<GetMessageByIdQuery, MessageResponse>
    {
        public async Task<Result<MessageResponse>> Handle(GetMessageByIdQuery query, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            Message? message = await context.Messages
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.Id == query.MessageId, cancellationToken);

            if (message is null)
            {
                return Result.Failure<MessageResponse>(MessageErrors.NotFound(query.MessageId));
            }

            bool isDirectUser =
                (message.SenderType == ChatParticipantType.User && message.SenderId == userId) ||
                (message.ReceiverType == ChatParticipantType.User && message.ReceiverId == userId);

            bool isStartupMember = false;
            if (!isDirectUser)
            {
                var startupIds = new List<Guid>(2);
                if (message.SenderType == ChatParticipantType.Startup) startupIds.Add(message.SenderId);
                if (message.ReceiverType == ChatParticipantType.Startup) startupIds.Add(message.ReceiverId);

                if (startupIds.Count > 0)
                {
                    isStartupMember = await context.StartupMembers.AnyAsync(
                        sm => sm.ProfileId == userId && startupIds.Contains(sm.StartupId),
                        cancellationToken);
                }
            }

            if (!isDirectUser && !isStartupMember)
            {
                return Result.Failure<MessageResponse>(MessageErrors.Unauthorized);
            }

            return new MessageResponse
            {
                Id = message.Id,
                SenderId = message.SenderId,
                SenderType = message.SenderType,
                ReceiverId = message.ReceiverId,
                ReceiverType = message.ReceiverType,
                TextContent = message.TextContent,
                MediaIds = message.MediaIds,
                MetricIds = message.MetricIds,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt,
                UpdatedAt = message.UpdatedAt
            };
        }
    }
}
