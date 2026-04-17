using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Messages;
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

            bool receiverExists = await context.Users.AnyAsync(u => u.Id == command.ReceiverId, cancellationToken);
            if (!receiverExists)
            {
                return Result.Failure<Guid>(UserErrors.NotFound(command.ReceiverId));
            }

            Message message = Message.Create(
                userContext.UserId,
                command.ReceiverId,
                command.TextContent,
                command.MediaIds,
                command.MetricIds,
                dateTimeProvider.UtcNow);

            context.Messages.Add(message);
            await context.SaveChangesAsync(cancellationToken);

            return message.Id;
        }
    }
}
