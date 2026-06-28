using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Scoring;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Infrastructure.Valuation
{
    /// <summary>
    /// Reads the (tiny, quarterly-changing) benchmark table once and caches the whole set in Redis;
    /// the as-of resolution happens in memory, so the cache key is date-independent. The admin write
    /// path invalidates <see cref="CacheKeys.ValuationBenchmarks"/> on every change.
    /// </summary>
    internal sealed class ValuationBenchmarkProvider(
        IApplicationDbContext context,
        ICacheService cache) : IValuationBenchmarkProvider
    {
        public async Task<ValuationBenchmarkSet> GetAsync(DateTime asOf, CancellationToken cancellationToken)
        {
            string key = CacheKeys.ValuationBenchmarks();

            List<ValuationBenchmarkRow>? rows = await cache.GetAsync<List<ValuationBenchmarkRow>>(key, cancellationToken);
            if (rows is null)
            {
                rows = await context.ValuationBenchmarks
                    .AsNoTracking()
                    .Select(b => new ValuationBenchmarkRow(b.MetricType, b.Industry, b.Stage, b.Value, b.EffectiveFrom))
                    .ToListAsync(cancellationToken);

                await cache.SetAsync(key, rows, CacheTtl.Default, cancellationToken);
            }

            return ValuationBenchmarkSet.FromRows(rows, asOf);
        }
    }
}
