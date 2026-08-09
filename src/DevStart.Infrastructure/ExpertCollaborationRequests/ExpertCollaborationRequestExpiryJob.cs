using DevStart.Application.Abstractions.Data;
using DevStart.Application.ExpertCollaborationRequests;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevStart.Infrastructure.ExpertCollaborationRequests
{
    /// <summary>
    /// Recurring Hangfire job that times out collaboration requests nobody answered within
    /// <see cref="ExpertCollaborationOptions.PendingTtlDays"/>. Expiring them frees the unique pending
    /// pair so the initiator can try again, and clears both inboxes of stale rows. Each expiry raises a
    /// domain event, so the notification is published by the usual SaveChanges dispatch.
    /// </summary>
    public sealed class ExpertCollaborationRequestExpiryJob(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IOptions<ExpertCollaborationOptions> options,
        ILogger<ExpertCollaborationRequestExpiryJob> logger)
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            int ttlDays = options.Value.PendingTtlDays;

            if (ttlDays <= 0)
            {
                return;
            }

            DateTime utcNow = dateTimeProvider.UtcNow;
            DateTime cutoff = utcNow.AddDays(-ttlDays);

            List<ExpertCollaborationRequest> staleRequests = await context.ExpertCollaborationRequests
                .Where(r => r.Status == ExpertCollaborationRequestStatus.Pending && r.CreatedAt <= cutoff)
                .ToListAsync(cancellationToken);

            if (staleRequests.Count == 0)
            {
                return;
            }

            foreach (ExpertCollaborationRequest request in staleRequests)
            {
                Result expire = request.Expire(utcNow);

                if (expire.IsFailure)
                {
                    logger.LogWarning(
                        "Expiry skipped for expert collaboration request {RequestId}: {Error}",
                        request.Id,
                        expire.Error.Code);
                    continue;
                }

                request.Raise(new ExpertCollaborationRequestExpiredDomainEvent(
                    request.Id,
                    request.ExpertProfileId,
                    request.StartupId,
                    request.Initiator));
            }

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Expired {Count} expert collaboration request(s) pending since before {Cutoff:u}.",
                staleRequests.Count,
                cutoff);
        }
    }
}
