using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.AccountDeletion;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.AccountDeletion.CancelDeletion
{
    internal sealed class CancelAccountDeletionCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CancelAccountDeletionCommand>
    {
        public async Task<Result> Handle(
            CancelAccountDeletionCommand command,
            CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            AccountDeletionRequest? request = await context.AccountDeletionRequests
                .SingleOrDefaultAsync(
                    r => r.UserId == userId && r.Status == AccountDeletionRequestStatus.Pending,
                    cancellationToken);

            if (request is null)
            {
                return Result.Failure(AccountDeletionErrors.NotRequested);
            }

            Result cancelled = request.Cancel(dateTimeProvider.UtcNow);
            if (cancelled.IsFailure)
            {
                return cancelled;
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
