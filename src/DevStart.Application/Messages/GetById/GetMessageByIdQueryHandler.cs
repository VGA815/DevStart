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

            if (!await MessageAccess.CanReadAsync(context, message, userId, cancellationToken))
            {
                return Result.Failure<MessageResponse>(MessageErrors.Unauthorized);
            }

            // The author behind a startup message is disclosed to that startup's own side only.
            bool isOwnStartupMessage =
                message.SenderType == ChatParticipantType.Startup &&
                message.SentByProfileId is not null &&
                await StartupIdentity.CanActAsAsync(context, message.SenderId, userId, cancellationToken);

            return new MessageResponse
            {
                Id = message.Id,
                SenderId = message.SenderId,
                SenderType = message.SenderType,
                SentByProfileId = isOwnStartupMessage ? message.SentByProfileId : null,
                ReceiverId = message.ReceiverId,
                ReceiverType = message.ReceiverType,
                TextContent = message.TextContent,
                MediaIds = message.MediaIds,
                MetricIds = message.MetricIds,
                DocumentIds = message.DocumentIds,
                FileIds = message.FileIds,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt,
                UpdatedAt = message.UpdatedAt
            };
        }
    }
}
