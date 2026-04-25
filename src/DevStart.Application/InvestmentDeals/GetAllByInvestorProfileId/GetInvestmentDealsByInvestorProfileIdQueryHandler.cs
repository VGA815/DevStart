using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentDeals;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.InvestmentDeals.GetAllByInvestorProfileId
{
    internal sealed class GetInvestmentDealsByInvestorProfileIdQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : IQueryHandler<GetInvestmentDealsByInvestorProfileIdQuery, List<InvestmentDealResponse>>
    {
        public async Task<Result<List<InvestmentDealResponse>>> Handle(GetInvestmentDealsByInvestorProfileIdQuery query, CancellationToken cancellationToken)
        {
            if (query.InvestorProfileId != userContext.UserId)
            {
                return Result.Failure<List<InvestmentDealResponse>>(InvestmentDealErrors.Unauthorized);
            }

            List<InvestmentDealResponse> deals = await context.InvestmentDeals
                .AsNoTracking()
                .Where(d => d.InvestorProfileId == query.InvestorProfileId)
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
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    CompletedAt = d.CompletedAt
                })
                .ToListAsync(cancellationToken);

            return deals;
        }
    }
}
