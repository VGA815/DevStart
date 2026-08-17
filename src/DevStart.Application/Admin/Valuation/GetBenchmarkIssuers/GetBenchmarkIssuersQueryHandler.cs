using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Valuation.GetBenchmarkIssuers
{
    internal sealed class GetBenchmarkIssuersQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetBenchmarkIssuersQuery, List<BenchmarkIssuerResponse>>
    {
        public async Task<Result<List<BenchmarkIssuerResponse>>> Handle(
            GetBenchmarkIssuersQuery query,
            CancellationToken cancellationToken)
        {
            List<BenchmarkIssuer> issuers = await context.BenchmarkIssuers
                .AsNoTracking()
                .OrderBy(i => i.Industry)
                .ThenBy(i => i.Ticker)
                .ToListAsync(cancellationToken);

            // Dozens of issuers, a handful of observations each — one pull and reduce in memory beats
            // a correlated subquery per row.
            List<BenchmarkObservation> observations = await context.BenchmarkObservations
                .AsNoTracking()
                .Where(o => o.IssuerId != null)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, List<BenchmarkObservation>> byIssuer = observations
                .GroupBy(o => o.IssuerId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            return issuers.Select(i =>
            {
                byIssuer.TryGetValue(i.Id, out List<BenchmarkObservation>? mine);
                BenchmarkObservation? cap = mine?
                    .Where(o => o.Metric == BenchmarkObservationMetric.MarketCap)
                    .OrderByDescending(o => o.AsOf)
                    .FirstOrDefault();
                BenchmarkObservation? revenue = mine?
                    .Where(o => o.Metric == BenchmarkObservationMetric.Revenue)
                    .OrderByDescending(o => o.FiscalYear ?? 0)
                    .FirstOrDefault();

                // The override is what the derivation would actually use, so it is what we show.
                bool manual = i.RevenueOverride is not null;

                return new BenchmarkIssuerResponse
                {
                    Id = i.Id,
                    Ticker = i.Ticker,
                    Inn = i.Inn,
                    DisplayName = i.DisplayName,
                    Industry = i.Industry,
                    IsActive = i.IsActive,
                    RevenueOverride = i.RevenueOverride,
                    RevenueOverrideFiscalYear = i.RevenueOverrideFiscalYear,
                    RevenueOverrideNote = i.RevenueOverrideNote,
                    Note = i.Note,
                    LatestMarketCap = cap?.Value,
                    LatestMarketCapAsOf = cap?.AsOf,
                    LatestRevenue = manual ? i.RevenueOverride : revenue?.Value,
                    LatestRevenueFiscalYear = manual ? i.RevenueOverrideFiscalYear : revenue?.FiscalYear,
                    LatestRevenueIsManual = manual,
                };
            }).ToList();
        }
    }
}
