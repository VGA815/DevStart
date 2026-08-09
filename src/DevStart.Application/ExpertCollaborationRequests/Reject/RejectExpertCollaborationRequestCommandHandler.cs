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

            // Only the side that owes an answer may reject: the startup for an expert's application,
            // the expert for a startup's invitation.
            if (!await ExpertCollaborationRequestParticipants.CanRespondAsync(
                    request, userContext.UserId, authorization, cancellationToken))
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
                request.StartupId,
                request.Initiator));

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
