using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Validation;
using DevStart.Domain.DealDocuments;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentDeals.GetById
{
    internal sealed class GetInvestmentDealByIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDealTermsValidator dealTermsValidator)
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

            IReadOnlyList<DealTermsFlag> flags = dealTermsValidator.Validate(new DealTermsInput(
                deal.Instrument, deal.Amount, deal.ValuationCap, deal.Discount,
                deal.InterestRate, deal.TermMonths, deal.PreMoneyValuation,
                deal.LiquidationPreference, deal.ProRataRights));

            string startupName = await context.Startups
                .AsNoTracking()
                .Where(s => s.Id == deal.StartupId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            string investorDisplayName = await context.InvestorProfiles
                .AsNoTracking()
                .Where(ip => ip.Id == deal.InvestorProfileId)
                .Select(ip => ip.DisplayName)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            string? roadmapItemTitle = deal.RoadmapItemId.HasValue
                ? await context.StartupRoadmapItems
                    .AsNoTracking()
                    .Where(r => r.Id == deal.RoadmapItemId.Value)
                    .Select(r => r.Title)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            bool documentsReady = await context.DealDocuments
                .AsNoTracking()
                .AnyAsync(d => d.DealId == deal.Id, cancellationToken);

            return new InvestmentDealResponse
            {
                Id = deal.Id,
                ApplicationId = deal.ApplicationId,
                InvestorProfileId = deal.InvestorProfileId,
                InvestorDisplayName = investorDisplayName,
                StartupId = deal.StartupId,
                StartupName = startupName,
                RoadmapItemId = deal.RoadmapItemId,
                RoadmapItemTitle = roadmapItemTitle,
                Amount = deal.Amount,
                ConfirmedByStartup = deal.ConfirmedByStartup,
                ConfirmedByInvestor = deal.ConfirmedByInvestor,
                Status = deal.Status,
                Instrument = deal.Instrument,
                ValuationCap = deal.ValuationCap,
                Discount = deal.Discount,
                InterestRate = deal.InterestRate,
                TermMonths = deal.TermMonths,
                PreMoneyValuation = deal.PreMoneyValuation,
                LiquidationPreference = deal.LiquidationPreference,
                ProRataRights = deal.ProRataRights,
                Flags = flags,
                DocumentsReady = documentsReady,
                CreatedAt = deal.CreatedAt,
                UpdatedAt = deal.UpdatedAt,
                CompletedAt = deal.CompletedAt
            };
        }
    }
}
