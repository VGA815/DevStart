using DevStart.Application.Abstractions.Data;
using DevStart.Application.Startups;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.StartupMembers;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ExpertCollaborationRequests
{
    /// <summary>
    /// Resolves "which side of a request is this user on" for both directions. A request always has an
    /// initiator side and a responder side: the initiator may withdraw, the responder may accept or
    /// reject. Keeping the mapping here stops the three command handlers from drifting apart.
    /// </summary>
    internal static class ExpertCollaborationRequestParticipants
    {
        /// <summary>
        /// True when <paramref name="userId"/> is on the side that owes an answer.
        /// </summary>
        public static Task<bool> CanRespondAsync(
            ExpertCollaborationRequest request,
            Guid userId,
            IStartupAuthorizationService authorization,
            CancellationToken cancellationToken)
            => request.AwaitsExpertResponse
                ? Task.FromResult(request.ExpertProfileId == userId)
                : authorization.IsFounderOrAdminAsync(userId, request.StartupId, cancellationToken);

        /// <summary>
        /// True when <paramref name="userId"/> is on the side that opened the request.
        /// </summary>
        public static Task<bool> CanWithdrawAsync(
            ExpertCollaborationRequest request,
            Guid userId,
            IStartupAuthorizationService authorization,
            CancellationToken cancellationToken)
            => request.AwaitsExpertResponse
                ? authorization.IsFounderOrAdminAsync(userId, request.StartupId, cancellationToken)
                : Task.FromResult(request.ExpertProfileId == userId);

        /// <summary>
        /// Users who should be notified on the startup side — everyone who can act on the request.
        /// </summary>
        public static async Task<List<Guid>> GetStartupRecipientsAsync(
            IApplicationDbContext context,
            Guid startupId,
            CancellationToken cancellationToken)
            => await context.StartupMembers
                .AsNoTracking()
                .Where(sm => sm.StartupId == startupId
                          && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration))
                .Select(sm => sm.ProfileId)
                .ToListAsync(cancellationToken);

        /// <summary>
        /// Recipients on the side that did not perform <paramref name="initiator"/>'s action — used for
        /// "your counterparty did something" notifications.
        /// </summary>
        public static Task<List<Guid>> GetResponderRecipientsAsync(
            IApplicationDbContext context,
            Guid startupId,
            Guid expertProfileId,
            CollaborationRequestInitiator initiator,
            CancellationToken cancellationToken)
            => initiator == CollaborationRequestInitiator.Expert
                ? GetStartupRecipientsAsync(context, startupId, cancellationToken)
                : Task.FromResult(new List<Guid> { expertProfileId });

        /// <summary>
        /// Recipients on the side that opened the request.
        /// </summary>
        public static Task<List<Guid>> GetInitiatorRecipientsAsync(
            IApplicationDbContext context,
            Guid startupId,
            Guid expertProfileId,
            CollaborationRequestInitiator initiator,
            CancellationToken cancellationToken)
            => initiator == CollaborationRequestInitiator.Expert
                ? Task.FromResult(new List<Guid> { expertProfileId })
                : GetStartupRecipientsAsync(context, startupId, cancellationToken);
    }
}
