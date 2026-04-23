using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Messages;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Messages.Create
{
    internal sealed class CreateMessageCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateMessageCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateMessageCommand command, CancellationToken cancellationToken)
        {
            if (command.TextContent is null
                && (command.MetricIds is null || command.MetricIds.Count == 0)
                && (command.MediaIds is null || command.MediaIds.Count == 0))
            {
                return Result.Failure<Guid>(MessageErrors.IsEmpty);
            }

            Guid senderId;
            ChatParticipantType senderType;

            if (command.SenderStartupId.HasValue)
            {
                bool isMember = await context.StartupMembers.AnyAsync(
                    sm => sm.StartupId == command.SenderStartupId.Value && sm.ProfileId == userContext.UserId,
                    cancellationToken);

                if (!isMember)
                {
                    return Result.Failure<Guid>(MessageErrors.Unauthorized);
                }

                senderId = command.SenderStartupId.Value;
                senderType = ChatParticipantType.Startup;
            }
            else
            {
                senderId = userContext.UserId;
                senderType = ChatParticipantType.User;
            }

            if (senderType == command.ReceiverType && senderId == command.ReceiverId)
            {
                return Result.Failure<Guid>(MessageErrors.Unauthorized);
            }

            bool receiverExists = command.ReceiverType switch
            {
                ChatParticipantType.User => await context.Users.AnyAsync(u => u.Id == command.ReceiverId, cancellationToken),
                ChatParticipantType.Startup => await context.Startups.AnyAsync(s => s.Id == command.ReceiverId, cancellationToken),
                _ => false
            };

            if (!receiverExists)
            {
                return Result.Failure<Guid>(MessageErrors.ReceiverNotFound(command.ReceiverId, command.ReceiverType));
            }

            Message message = Message.Create(
                senderId,
                senderType,
                command.ReceiverId,
                command.ReceiverType,
                command.TextContent,
                command.MediaIds,
                command.MetricIds,
                dateTimeProvider.UtcNow);

            message.Raise(new MessageCreatedDomainEvent(
                message.Id,
                message.SenderId,
                message.SenderType,
                message.ReceiverId,
                message.ReceiverType));

            context.Messages.Add(message);
            await context.SaveChangesAsync(cancellationToken);

            return message.Id;
        }
    }
}
