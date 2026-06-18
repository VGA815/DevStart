using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Abstractions.Validation;
using DevStart.Domain.InvestmentApplications;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentApplications.GetAllByInvestorProfileId
{
    internal sealed class GetInvestmentApplicationsByInvestorProfileIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDealTermsValidator dealTermsValidator)
        : IQueryHandler<GetInvestmentApplicationsByInvestorProfileIdQuery, List<InvestmentApplicationResponse>>
    {
        public async Task<Result<List<InvestmentApplicationResponse>>> Handle(GetInvestmentApplicationsByInvestorProfileIdQuery query, CancellationToken cancellationToken)
        {
            if (query.InvestorProfileId != userContext.UserId)
            {
                return Result.Failure<List<InvestmentApplicationResponse>>(InvestmentApplicationErrors.Unauthorized);
            }

            List<InvestmentApplicationResponse> applications = await context.InvestmentApplications
                .AsNoTracking()
                .Where(a => a.InvestorProfileId == query.InvestorProfileId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new InvestmentApplicationResponse
                {
                    Id = a.Id,
                    InvestorProfileId = a.InvestorProfileId,
                    InvestorDisplayName = context.Profiles
                        .Where(p => p.UserId == a.InvestorProfileId)
                        .Select(p => p.Name)
                        .FirstOrDefault() ?? string.Empty,
                    StartupId = a.StartupId,
                    StartupName = context.Startups
                        .Where(s => s.Id == a.StartupId)
                        .Select(s => s.Name)
                        .FirstOrDefault() ?? string.Empty,
                    RoadmapItemId = a.RoadmapItemId,
                    RoadmapItemTitle = a.RoadmapItemId != null
                        ? context.StartupRoadmapItems
                            .Where(r => r.Id == a.RoadmapItemId)
                            .Select(r => r.Title)
                            .FirstOrDefault()
                        : null,
                    Amount = a.Amount,
                    Message = a.Message,
                    Status = a.Status,
                    Instrument = a.Instrument,
                    ValuationCap = a.ValuationCap,
                    Discount = a.Discount,
                    InterestRate = a.InterestRate,
                    TermMonths = a.TermMonths,
                    PreMoneyValuation = a.PreMoneyValuation,
                    LiquidationPreference = a.LiquidationPreference,
                    ProRataRights = a.ProRataRights,
                    CreatedAt = a.CreatedAt,
                    UpdatedAt = a.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            foreach (InvestmentApplicationResponse a in applications)
            {
                a.Flags = dealTermsValidator.Validate(new DealTermsInput(
                    a.Instrument, a.Amount, a.ValuationCap, a.Discount,
                    a.InterestRate, a.TermMonths, a.PreMoneyValuation,
                    a.LiquidationPreference, a.ProRataRights));
            }

            return applications;
        }
    }
}
