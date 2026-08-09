using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests.Accept
{
    internal sealed class AcceptExpertCollaborationRequestCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IStartupAuthorizationService authorization,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<AcceptExpertCollaborationRequestCommand>
    {
        public async Task<Result> Handle(AcceptExpertCollaborationRequestCommand command, CancellationToken cancellationToken)
        {
            ExpertCollaborationRequest? request = await context.ExpertCollaborationRequests
                .SingleOrDefaultAsync(r => r.Id == command.RequestId, cancellationToken);

            if (request is null)
            {
                return Result.Failure(ExpertCollaborationRequestErrors.NotFound(command.RequestId));
            }

            // Only the side that owes an answer may accept: the startup for an expert's application,
            // the expert for a startup's invitation.
            if (!await ExpertCollaborationRequestParticipants.CanRespondAsync(
                    request, userContext.UserId, authorization, cancellationToken))
            {
                return Result.Failure(ExpertCollaborationRequestErrors.Unauthorized);
            }

            Result acceptResult = request.Accept(dateTimeProvider.UtcNow);

            if (acceptResult.IsFailure)
            {
                return acceptResult;
            }

            request.Raise(new ExpertCollaborationRequestAcceptedDomainEvent(
                request.Id,
                request.ExpertProfileId,
                request.StartupId,
                request.Initiator));

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
