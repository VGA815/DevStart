using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests.Reject
{
    internal sealed class RejectExpertCollaborationRequestCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IStartupAuthorizationService authorization,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<RejectExpertCollaborationRequestCommand>
    {
        public async Task<Result> Handle(RejectExpertCollaborationRequestCommand command, CancellationToken cancellationToken)
        {
            ExpertCollaborationRequest? request = await context.ExpertCollaborationRequests
                .SingleOrDefaultAsync(r => r.Id == command.RequestId, cancellationToken);

            if (request is null)
            {
                return Result.Failure(ExpertCollaborationRequestErrors.NotFound(command.RequestId));
            }

            if (!await authorization.IsFounderOrAdminAsync(userContext.UserId, request.StartupId, cancellationToken))
            {
                return Result.Failure(ExpertCollaborationRequestErrors.Unauthorized);
            }

            Result rejectResult = request.Reject(dateTimeProvider.UtcNow);

            if (rejectResult.IsFailure)
            {
                return rejectResult;
            }

            request.Raise(new ExpertCollaborationRequestRejectedDomainEvent(
                request.Id,
                request.ExpertProfileId,
                request.StartupId));

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
