using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Valuation.GetValuationBenchmarks
{
    internal sealed class GetValuationBenchmarksQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetValuationBenchmarksQuery, List<ValuationBenchmarkResponse>>
    {
        public async Task<Result<List<ValuationBenchmarkResponse>>> Handle(
            GetValuationBenchmarksQuery query,
            CancellationToken cancellationToken)
        {
            DateTime asOf = query.AsOf ?? dateTimeProvider.UtcNow;

            // Tiny table — pull the effective versions and reduce to the latest-per-key in memory.
            List<ValuationBenchmark> effective = await context.ValuationBenchmarks
                .AsNoTracking()
                .Where(b => b.EffectiveFrom <= asOf)
                .ToListAsync(cancellationToken);

            List<ValuationBenchmarkResponse> current = effective
                .GroupBy(b => new { b.MetricType, b.Industry, b.Stage })
                .Select(g => g.OrderByDescending(b => b.EffectiveFrom).First())
                .OrderBy(b => b.MetricType)
                .ThenBy(b => b.Industry)
                .ThenBy(b => b.Stage)
                .Select(Map)
                .ToList();

            return current;
        }

        private static ValuationBenchmarkResponse Map(ValuationBenchmark b) => new()
        {
            Id = b.Id,
            MetricType = b.MetricType,
            Industry = b.Industry,
            Stage = b.Stage,
            Value = b.Value,
            Currency = b.Currency,
            EffectiveFrom = b.EffectiveFrom,
            Source = b.Source,
            CreatedAt = b.CreatedAt,
            CreatedByUserId = b.CreatedByUserId,
        };
    }
}
