using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentDeals.GetById
{
    internal sealed class GetInvestmentDealByIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : IQueryHandler<GetInvestmentDealByIdQuery, InvestmentDealResponse>
    {
        public async Task<Result<InvestmentDealResponse>> Handle(GetInvestmentDealByIdQuery query, CancellationToken cancellationToken)
        {
            InvestmentDeal? deal = await context.InvestmentDeals
                .AsNoTracking()
                .SingleOrDefaultAsync(d => d.Id == query.DealId, cancellationToken);

            if (deal is null)
            {
                return Result.Failure<InvestmentDealResponse>(InvestmentDealErrors.NotFound(query.DealId));
            }

            Guid userId = userContext.UserId;
            bool isInvestor = deal.InvestorProfileId == userId;
            bool isFounderOrAdmin = false;

            if (!isInvestor)
            {
                isFounderOrAdmin = await context.StartupMembers
                    .AsNoTracking()
                    .AnyAsync(sm => sm.StartupId == deal.StartupId
                                 && sm.ProfileId == userId
                                 && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration),
                              cancellationToken);
            }

            if (!isInvestor && !isFounderOrAdmin)
            {
                return Result.Failure<InvestmentDealResponse>(InvestmentDealErrors.Unauthorized);
            }

            return new InvestmentDealResponse
            {
                Id = deal.Id,
                ApplicationId = deal.ApplicationId,
                InvestorProfileId = deal.InvestorProfileId,
                StartupId = deal.StartupId,
                RoadmapItemId = deal.RoadmapItemId,
                Amount = deal.Amount,
                ConfirmedByStartup = deal.ConfirmedByStartup,
                ConfirmedByInvestor = deal.ConfirmedByInvestor,
                Status = deal.Status,
                CreatedAt = deal.CreatedAt,
                UpdatedAt = deal.UpdatedAt,
                CompletedAt = deal.CompletedAt
            };
        }
    }
}
