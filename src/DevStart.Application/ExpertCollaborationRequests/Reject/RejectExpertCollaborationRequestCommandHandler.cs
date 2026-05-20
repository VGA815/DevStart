using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests.Reject
{
    internal sealed class RejectExpertCollaborationRequestCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
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

            StartupMember? member = await context.StartupMembers
                .SingleOrDefaultAsync(
                    sm => sm.StartupId == request.StartupId && sm.ProfileId == userContext.UserId,
                    cancellationToken);

            if (member is null || member.Role == StartupRole.Member)
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
