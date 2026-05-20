using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests.Withdraw
{
    internal sealed class WithdrawExpertCollaborationRequestCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<WithdrawExpertCollaborationRequestCommand>
    {
        public async Task<Result> Handle(WithdrawExpertCollaborationRequestCommand command, CancellationToken cancellationToken)
        {
            ExpertCollaborationRequest? request = await context.ExpertCollaborationRequests
                .SingleOrDefaultAsync(r => r.Id == command.RequestId, cancellationToken);

            if (request is null)
            {
                return Result.Failure(ExpertCollaborationRequestErrors.NotFound(command.RequestId));
            }

            if (request.ExpertProfileId != userContext.UserId)
            {
                return Result.Failure(ExpertCollaborationRequestErrors.Unauthorized);
            }

            Result withdrawResult = request.Withdraw(dateTimeProvider.UtcNow);

            if (withdrawResult.IsFailure)
            {
                return withdrawResult;
            }

            request.Raise(new ExpertCollaborationRequestWithdrawnDomainEvent(
                request.Id,
                request.ExpertProfileId,
                request.StartupId));

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
