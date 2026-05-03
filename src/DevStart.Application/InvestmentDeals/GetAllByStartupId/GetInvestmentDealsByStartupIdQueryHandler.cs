using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Validation;
using DevStart.Domain.InvestmentDeals;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentDeals.GetAllByStartupId
{
    internal sealed class GetInvestmentDealsByStartupIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDealTermsValidator dealTermsValidator)
        : IQueryHandler<GetInvestmentDealsByStartupIdQuery, List<InvestmentDealResponse>>
    {
        public async Task<Result<List<InvestmentDealResponse>>> Handle(GetInvestmentDealsByStartupIdQuery query, CancellationToken cancellationToken)
        {
            bool isFounderOrAdmin = await context.StartupMembers
                .AsNoTracking()
                .AnyAsync(sm => sm.StartupId == query.StartupId
                             && sm.ProfileId == userContext.UserId
                             && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration),
                          cancellationToken);

            if (!isFounderOrAdmin)
            {
                return Result.Failure<List<InvestmentDealResponse>>(InvestmentDealErrors.Unauthorized);
            }

            List<InvestmentDealResponse> deals = await context.InvestmentDeals
                .AsNoTracking()
                .Where(d => d.StartupId == query.StartupId)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new InvestmentDealResponse
                {
                    Id = d.Id,
                    ApplicationId = d.ApplicationId,
                    InvestorProfileId = d.InvestorProfileId,
                    InvestorDisplayName = context.InvestorProfiles
                        .Where(ip => ip.Id == d.InvestorProfileId)
                        .Select(ip => ip.DisplayName)
                        .FirstOrDefault() ?? string.Empty,
                    StartupId = d.StartupId,
                    StartupName = context.Startups
                        .Where(s => s.Id == d.StartupId)
                        .Select(s => s.Name)
                        .FirstOrDefault() ?? string.Empty,
                    RoadmapItemId = d.RoadmapItemId,
                    RoadmapItemTitle = d.RoadmapItemId != null
                        ? context.StartupRoadmapItems
                            .Where(r => r.Id == d.RoadmapItemId)
                            .Select(r => r.Title)
                            .FirstOrDefault()
                        : null,
                    Amount = d.Amount,
                    ConfirmedByStartup = d.ConfirmedByStartup,
                    ConfirmedByInvestor = d.ConfirmedByInvestor,
                    Status = d.Status,
                    Instrument = d.Instrument,
                    ValuationCap = d.ValuationCap,
                    Discount = d.Discount,
                    InterestRate = d.InterestRate,
                    TermMonths = d.TermMonths,
                    PreMoneyValuation = d.PreMoneyValuation,
                    LiquidationPreference = d.LiquidationPreference,
                    ProRataRights = d.ProRataRights,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    CompletedAt = d.CompletedAt
                })
                .ToListAsync(cancellationToken);

            foreach (InvestmentDealResponse d in deals)
            {
                d.Flags = dealTermsValidator.Validate(new DealTermsInput(
                    d.Instrument, d.Amount, d.ValuationCap, d.Discount,
                    d.InterestRate, d.TermMonths, d.PreMoneyValuation,
                    d.LiquidationPreference, d.ProRataRights));
            }

            return deals;
        }
    }
}
