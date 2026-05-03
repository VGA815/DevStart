using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.BackgroundJobs;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentDeals.RegenerateDocuments
{
    internal sealed class RegenerateDealDocumentsCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IBackgroundJobScheduler backgroundJobScheduler)
        : ICommandHandler<RegenerateDealDocumentsCommand>
    {
        public async Task<Result> Handle(RegenerateDealDocumentsCommand command, CancellationToken cancellationToken)
        {
            InvestmentDeal? deal = await context.InvestmentDeals
                .AsNoTracking()
                .SingleOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);

            if (deal is null)
            {
                return Result.Failure(InvestmentDealErrors.NotFound(command.DealId));
            }

            // Only the investor or a startup founder/admin may request regeneration.
            Guid userId = userContext.UserId;
            bool isInvestor = deal.InvestorProfileId == userId;
            bool isFounderOrAdmin = !isInvestor && await context.StartupMembers
                .AsNoTracking()
                .AnyAsync(sm => sm.StartupId == deal.StartupId
                             && sm.ProfileId == userId
                             && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration),
                          cancellationToken);

            if (!isInvestor && !isFounderOrAdmin)
            {
                return Result.Failure(InvestmentDealErrors.Unauthorized);
            }

            // The job itself is idempotent — it exits early if DealDocument already exists.
            backgroundJobScheduler.EnqueueTermSheetGeneration(deal.Id);

            return Result.Success();
        }
    }
}
