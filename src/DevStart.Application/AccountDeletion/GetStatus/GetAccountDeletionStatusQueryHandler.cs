using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.AccountDeletion;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.AccountDeletion.GetStatus
{
    internal sealed class GetAccountDeletionStatusQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : IQueryHandler<GetAccountDeletionStatusQuery, AccountDeletionStatusResponse>
    {
        public async Task<Result<AccountDeletionStatusResponse>> Handle(
            GetAccountDeletionStatusQuery query,
            CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            AccountDeletionRequest? request = await context.AccountDeletionRequests
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.UserId == userId && r.Status == AccountDeletionRequestStatus.Pending,
                    cancellationToken);

            List<AffectedStartupResponse> startups = await context.Startups
                .AsNoTracking()
                .Where(s => SoleFounderStartups.IdsFor(context, userId).Contains(s.Id))
                .Select(s => new AffectedStartupResponse(s.Id, s.Name))
                .ToListAsync(cancellationToken);

            return new AccountDeletionStatusResponse(
                Pending: request is not null,
                RequestedAt: request?.RequestedAt,
                ScheduledFor: request?.ScheduledFor,
                StartupsToDelete: startups);
        }
    }
}
