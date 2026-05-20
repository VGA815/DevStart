using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests.Create
{
    internal sealed class CreateExpertCollaborationRequestCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateExpertCollaborationRequestCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateExpertCollaborationRequestCommand command, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            bool hasExpertProfile = await context.ExpertProfiles
                .AnyAsync(ep => ep.UserId == userId, cancellationToken);

            if (!hasExpertProfile)
            {
                return Result.Failure<Guid>(ExpertCollaborationRequestErrors.ExpertProfileRequired);
            }

            bool startupExists = await context.Startups
                .AnyAsync(s => s.Id == command.StartupId, cancellationToken);

            if (!startupExists)
            {
                return Result.Failure<Guid>(StartupErrors.NotFound(command.StartupId));
            }

            bool isMember = await context.StartupMembers
                .AnyAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == userId, cancellationToken);

            if (isMember)
            {
                return Result.Failure<Guid>(ExpertCollaborationRequestErrors.CannotApplyToOwnStartup);
            }

            bool hasPendingRequest = await context.ExpertCollaborationRequests
                .AnyAsync(r => r.ExpertProfileId == userId
                            && r.StartupId == command.StartupId
                            && r.Status == ExpertCollaborationRequestStatus.Pending,
                          cancellationToken);

            if (hasPendingRequest)
            {
                return Result.Failure<Guid>(ExpertCollaborationRequestErrors.AlreadyExistsForStartup);
            }

            if (command.ProposedHoursPerWeek.HasValue &&
                (command.ProposedHoursPerWeek.Value < 1 || command.ProposedHoursPerWeek.Value > 168))
            {
                return Result.Failure<Guid>(ExpertCollaborationRequestErrors.InvalidProposedHours);
            }

            if (command.ProposedRate.HasValue && command.ProposedRate.Value <= 0)
            {
                return Result.Failure<Guid>(ExpertCollaborationRequestErrors.InvalidProposedRate);
            }

            ExpertCollaborationRequest request = ExpertCollaborationRequest.Create(
                userId,
                command.StartupId,
                command.CollaborationType,
                command.Message,
                command.ProposedHoursPerWeek,
                command.ProposedRate,
                dateTimeProvider.UtcNow);

            request.Raise(new ExpertCollaborationRequestCreatedDomainEvent(
                request.Id,
                request.ExpertProfileId,
                request.StartupId,
                request.CollaborationType));

            context.ExpertCollaborationRequests.Add(request);
            await context.SaveChangesAsync(cancellationToken);

            return request.Id;
        }
    }
}
