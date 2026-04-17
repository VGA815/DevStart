using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Messages;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Messages.MarkAsRead
{
    internal sealed class MarkMessageAsReadCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<MarkMessageAsReadCommand>
    {
        public async Task<Result> Handle(MarkMessageAsReadCommand command, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            Message? message = await context.Messages
                .SingleOrDefaultAsync(m => m.Id == command.MessageId && m.ReceiverId == userId, cancellationToken);

            if (message is null)
            {
                return Result.Failure(MessageErrors.NotFound(command.MessageId));
            }

            message.MarkAsRead();
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
