using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InvestmentDeals;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Dashboard.GetPublicStats
{
    internal sealed class GetPublicStatsQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetPublicStatsQuery, PublicStatsResponse>
    {
        public async Task<Result<PublicStatsResponse>> Handle(GetPublicStatsQuery query, CancellationToken cancellationToken)
        {
            int startupsCount = await context.Startups.CountAsync(cancellationToken);
            int investorsCount = await context.InvestorProfiles.CountAsync(cancellationToken);
            int expertsCount = await context.ExpertProfiles.CountAsync(cancellationToken);

            decimal totalRaised = await context.InvestmentDeals
                .Where(d => d.Status == InvestmentDealStatus.Completed)
                .SumAsync(d => d.Amount, cancellationToken);

            return new PublicStatsResponse
            {
                StartupsCount = startupsCount,
                InvestorsCount = investorsCount,
                ExpertsCount = expertsCount,
                TotalRaised = totalRaised
            };
        }
    }
}
