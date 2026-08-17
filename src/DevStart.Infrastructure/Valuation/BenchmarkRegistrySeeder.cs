using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DevStart.Infrastructure.Valuation
{
    /// <summary>
    /// Seeds the two registry tables from <see cref="BenchmarkRegistryDefaults"/>, on the same pattern
    /// as <see cref="ValuationBenchmarksSeeder"/>: idempotent, existing rows skipped, never updated.
    /// Skipping rather than overwriting is the point — once an admin has curated a row (fixed a sector,
    /// added an INN, cleared <c>is_active</c>), a redeploy must not undo that.
    /// </summary>
    internal sealed class BenchmarkRegistrySeeder(
        IServiceProvider serviceProvider,
        IDateTimeProvider dateTimeProvider,
        ILogger<BenchmarkRegistrySeeder> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                IApplicationDbContext context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                DateTime now = dateTimeProvider.UtcNow;

                int issuersAdded = await SeedIssuersAsync(context, now, cancellationToken);
                int mappingsAdded = await SeedMappingsAsync(context, now, cancellationToken);

                if (issuersAdded > 0 || mappingsAdded > 0)
                {
                    await context.SaveChangesAsync(cancellationToken);
                    logger.LogInformation(
                        "Seeded benchmark registry: {Issuers} issuer(s), {Mappings} industry mapping(s).",
                        issuersAdded, mappingsAdded);
                }
            }
            catch (Exception exception)
            {
                // Same policy as the benchmark seeder: a failed seed must not stop the app from booting.
                logger.LogError(exception, "Failed to seed the benchmark registry.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static async Task<int> SeedIssuersAsync(
            IApplicationDbContext context, DateTime now, CancellationToken cancellationToken)
        {
            HashSet<string> existing = (await context.BenchmarkIssuers
                    .AsNoTracking()
                    .Select(i => i.Ticker)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            int added = 0;
            foreach (BenchmarkRegistryDefaults.IssuerSeed seed in BenchmarkRegistryDefaults.Issuers)
            {
                if (!existing.Add(seed.Ticker))
                {
                    continue;
                }

                context.BenchmarkIssuers.Add(BenchmarkRegistryDefaults.ToIssuer(seed, now));
                added++;
            }

            return added;
        }

        private static async Task<int> SeedMappingsAsync(
            IApplicationDbContext context, DateTime now, CancellationToken cancellationToken)
        {
            HashSet<string> existing = (await context.BenchmarkIndustryMappings
                    .AsNoTracking()
                    .Where(m => m.SourceKind == BenchmarkMappingSourceKind.Damodaran)
                    .Select(m => m.ExternalKey)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            int added = 0;
            foreach (BenchmarkRegistryDefaults.MappingSeed seed in BenchmarkRegistryDefaults.DamodaranBuckets)
            {
                if (!existing.Add(seed.ExternalKey))
                {
                    continue;
                }

                context.BenchmarkIndustryMappings.Add(BenchmarkRegistryDefaults.ToMapping(seed, now));
                added++;
            }

            return added;
        }
    }
}
