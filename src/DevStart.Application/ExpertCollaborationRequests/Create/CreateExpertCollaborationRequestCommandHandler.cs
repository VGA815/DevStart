using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.Application.ExpertCollaborationRequests.Create
{
    internal sealed class CreateExpertCollaborationRequestCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IStartupAuthorizationService authorization,
        IDateTimeProvider dateTimeProvider,
        IOptions<ExpertCollaborationOptions> options)
        : ICommandHandler<CreateExpertCollaborationRequestCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateExpertCollaborationRequestCommand command, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;
            DateTime utcNow = dateTimeProvider.UtcNow;

            StartupAvailability? startup = await context.Startups
                .AsNoTracking()
                .Where(s => s.Id == command.StartupId)
                .Select(s => new StartupAvailability(s.IsBanned, s.BanExpiresAt))
                .SingleOrDefaultAsync(cancellationToken);

            if (startup is null)
            {
                return Result.Failure<Guid>(StartupErrors.NotFound(command.StartupId));
            }

            // Mirrors PublicStartupVisibility, including lazy ban expiry: a banned startup neither
            // recruits experts nor collects applications.
            if (startup.IsBanned && (startup.BanExpiresAt is null || startup.BanExpiresAt > utcNow))
            {
                return Result.Failure<Guid>(ExpertCollaborationRequestErrors.StartupUnavailable);
            }

            // The direction is derived from the caller, never taken from the request body: whoever runs
            // the startup is inviting, anyone else is applying as an expert.
            bool callerRunsStartup = await authorization.IsFounderOrAdminAsync(userId, command.StartupId, cancellationToken);

            Guid expertProfileId;
            CollaborationRequestInitiator initiator;

            if (callerRunsStartup)
            {
                if (command.ExpertProfileId is not { } invitedExpertId || invitedExpertId == Guid.Empty)
                {
                    return Result.Failure<Guid>(ExpertCollaborationRequestErrors.ExpertProfileIdRequired);
                }

                expertProfileId = invitedExpertId;
                initiator = CollaborationRequestInitiator.Startup;
            }
            else
            {
                if (command.ExpertProfileId is { } claimedExpertId
                    && claimedExpertId != Guid.Empty
                    && claimedExpertId != userId)
                {
                    return Result.Failure<Guid>(ExpertCollaborationRequestErrors.Unauthorized);
                }

                expertProfileId = userId;
                initiator = CollaborationRequestInitiator.Expert;
            }

            bool expertProfileExists = await context.ExpertProfiles
                .AnyAsync(ep => ep.UserId == expertProfileId, cancellationToken);

            if (!expertProfileExists)
            {
                return Result.Failure<Guid>(initiator == CollaborationRequestInitiator.Expert
                    ? ExpertCollaborationRequestErrors.ExpertProfileRequired
                    : ExpertCollaborationRequestErrors.ExpertProfileNotFound);
            }

            bool expertIsMember = await context.StartupMembers
                .AnyAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == expertProfileId, cancellationToken);

            if (expertIsMember)
            {
                return Result.Failure<Guid>(initiator == CollaborationRequestInitiator.Expert
                    ? ExpertCollaborationRequestErrors.CannotApplyToOwnStartup
                    : ExpertCollaborationRequestErrors.ExpertAlreadyMember);
            }

            // One pending request per pair regardless of direction, so the two sides cannot open
            // mirrored requests at each other.
            bool hasPendingRequest = await context.ExpertCollaborationRequests
                .AnyAsync(r => r.ExpertProfileId == expertProfileId
                            && r.StartupId == command.StartupId
                            && r.Status == ExpertCollaborationRequestStatus.Pending,
                          cancellationToken);

            if (hasPendingRequest)
            {
                return Result.Failure<Guid>(ExpertCollaborationRequestErrors.AlreadyExistsForStartup);
            }

            Result cooldown = await CheckRejectionCooldownAsync(
                command.StartupId, expertProfileId, initiator, utcNow, cancellationToken);

            if (cooldown.IsFailure)
            {
                return Result.Failure<Guid>(cooldown.Error);
            }

            ExpertCollaborationRequest request = ExpertCollaborationRequest.Create(
                expertProfileId,
                command.StartupId,
                initiator,
                command.CollaborationType,
                command.Message,
                command.ProposedHoursPerWeek,
                command.ProposedRate,
                utcNow);

            request.Raise(new ExpertCollaborationRequestCreatedDomainEvent(
                request.Id,
                request.ExpertProfileId,
                request.StartupId,
                request.Initiator,
                request.CollaborationType));

            context.ExpertCollaborationRequests.Add(request);
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                context.ExpertCollaborationRequests.Remove(request);

                bool duplicatePendingRequestExists = await context.ExpertCollaborationRequests
                    .AsNoTracking()
                    .AnyAsync(r => r.ExpertProfileId == expertProfileId
                                && r.StartupId == command.StartupId
                                && r.Status == ExpertCollaborationRequestStatus.Pending,
                              cancellationToken);

                if (duplicatePendingRequestExists)
                {
                    return Result.Failure<Guid>(ExpertCollaborationRequestErrors.AlreadyExistsForStartup);
                }

                throw;
            }

            return request.Id;
        }

        /// <summary>
        /// Holds back only the side that was rejected. The rejecting side is free to reach out again
        /// immediately — it changed its mind, it is not being pestered.
        /// </summary>
        private async Task<Result> CheckRejectionCooldownAsync(
            Guid startupId,
            Guid expertProfileId,
            CollaborationRequestInitiator initiator,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            int cooldownDays = options.Value.RejectionCooldownDays;

            if (cooldownDays <= 0)
            {
                return Result.Success();
            }

            DateTime cooldownStart = utcNow.AddDays(-cooldownDays);

            DateTime? lastRejectedAt = await context.ExpertCollaborationRequests
                .AsNoTracking()
                .Where(r => r.ExpertProfileId == expertProfileId
                         && r.StartupId == startupId
                         && r.Initiator == initiator
                         && r.Status == ExpertCollaborationRequestStatus.Rejected
                         && r.UpdatedAt > cooldownStart)
                .MaxAsync(r => (DateTime?)r.UpdatedAt, cancellationToken);

            return lastRejectedAt is { } rejectedAt
                ? Result.Failure(ExpertCollaborationRequestErrors.RejectionCooldownActive(rejectedAt.AddDays(cooldownDays)))
                : Result.Success();
        }

        private sealed record StartupAvailability(bool IsBanned, DateTime? BanExpiresAt);
    }
}
