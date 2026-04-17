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
                .SingleOrDefaultAsync(
                    m => m.Id == query.MessageId && (m.SenderId == userId || m.ReceiverId == userId),
                    cancellationToken);

            if (message is null)
            {
                return Result.Failure<MessageResponse>(MessageErrors.NotFound(query.MessageId));
            }

            return new MessageResponse
            {
                Id = message.Id,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
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
