using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DevStart.Infrastructure.Valuation
{
    /// <summary>
    /// On startup, seeds the initial pre-money medians (the values that used to live in code as
    /// <c>ScorecardOptions.StageMedians</c>) so the engine is never left without medians the day the
    /// code fallback is removed. Idempotent: skips rows whose (metric, sector, stage, effective_from)
    /// already exist. Revenue multipliers are not seeded — they arrive via the admin API.
    /// </summary>
    internal sealed class ValuationBenchmarksSeeder(
        IServiceProvider serviceProvider,
        ILogger<ValuationBenchmarksSeeder> logger) : IHostedService
    {
        // Fixed start-of-validity for the seed version; later versions supersede via the admin API.
        private static readonly DateTime SeedEffectiveFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const string SeedSource = "initial seed";

        private static readonly (StartupStage Stage, decimal Value)[] StageMedians =
        [
            (StartupStage.Idea, 60_000_000m),
            (StartupStage.PreSeed, 120_000_000m),
            (StartupStage.Mvp, 250_000_000m),
            (StartupStage.Seed, 400_000_000m),
        ];

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                IApplicationDbContext context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                foreach ((StartupStage stage, decimal value) in StageMedians)
                {
                    bool alreadyExists = await context.ValuationBenchmarks.AnyAsync(
                        b => b.MetricType == BenchmarkMetricType.PreMoneyMedian
                            && b.Industry == Industry.Other
                            && b.Stage == stage
                            && b.EffectiveFrom == SeedEffectiveFrom,
                        cancellationToken);

                    if (alreadyExists)
                    {
                        continue;
                    }

                    context.ValuationBenchmarks.Add(ValuationBenchmark.Create(
                        BenchmarkMetricType.PreMoneyMedian,
                        Industry.Other,
                        stage,
                        value,
                        currency: "RUB",
                        effectiveFrom: SeedEffectiveFrom,
                        source: SeedSource,
                        createdByUserId: null,
                        utcNow: SeedEffectiveFrom));

                    await context.SaveChangesAsync(cancellationToken);

                    logger.LogInformation(
                        "Seeded valuation benchmark median {Stage} = {Value} ({Source})",
                        stage, value, SeedSource);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed valuation benchmarks");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
