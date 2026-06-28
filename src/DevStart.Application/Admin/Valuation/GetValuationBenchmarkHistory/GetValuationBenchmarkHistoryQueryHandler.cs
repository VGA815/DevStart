using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Valuation.GetValuationBenchmarkHistory
{
    internal sealed class GetValuationBenchmarkHistoryQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetValuationBenchmarkHistoryQuery, List<ValuationBenchmarkResponse>>
    {
        public async Task<Result<List<ValuationBenchmarkResponse>>> Handle(
            GetValuationBenchmarkHistoryQuery query,
            CancellationToken cancellationToken)
        {
            List<ValuationBenchmarkResponse> history = await context.ValuationBenchmarks
                .AsNoTracking()
                .Where(b => b.MetricType == query.MetricType
                    && b.Industry == query.Industry
                    && b.Stage == query.Stage)
                .OrderByDescending(b => b.EffectiveFrom)
                .Select(b => new ValuationBenchmarkResponse
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
                })
                .ToListAsync(cancellationToken);

            return history;
        }
    }
}
