using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.Domain.Valuation;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Admin.Valuation.AddValuationBenchmark
{
    internal sealed class AddValuationBenchmarkCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<AddValuationBenchmarkCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(AddValuationBenchmarkCommand command, CancellationToken cancellationToken)
        {
            // Append-only: the same (metric, sector, stage, effective_from) is a duplicate, not an edit.
            bool exists = await context.ValuationBenchmarks.AnyAsync(
                b => b.MetricType == command.MetricType
                    && b.Industry == command.Industry
                    && b.Stage == command.Stage
                    && b.EffectiveFrom == command.EffectiveFrom,
                cancellationToken);
            if (exists)
            {
                return Result.Failure<Guid>(ValuationBenchmarkErrors.DuplicateVersion);
            }

            DateTime now = dateTimeProvider.UtcNow;
            ValuationBenchmark benchmark = ValuationBenchmark.Create(
                command.MetricType,
                command.Industry,
                command.Stage,
                command.Value,
                command.Currency,
                command.EffectiveFrom,
                command.Source,
                userContext.UserId,
                now);

            context.ValuationBenchmarks.Add(benchmark);
            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.AddValuationBenchmark,
                AdminTargetType.ValuationBenchmark,
                benchmark.Id,
                $"Added {command.MetricType} benchmark for {command.Industry}"
                    + (command.Stage is { } s ? $"/{s}" : string.Empty)
                    + $" = {command.Value} (effective {command.EffectiveFrom:yyyy-MM-dd})",
                now));

            await context.SaveChangesAsync(cancellationToken);

            // Invalidate the cached benchmark set so the next valuation reads the new version, and the
            // cached startup scores with it: a benchmark row feeds both the competition sub-score and
            // the valuation, so leaving them would serve the old figures for up to an hour. Benchmark
            // writes are quarterly, so the broad eviction costs nothing in practice.
            await cacheService.RemoveAsync(CacheKeys.ValuationBenchmarks(), cancellationToken);
            await cacheService.RemoveByPrefixAsync(CacheKeys.StartupsPrefix(), cancellationToken);

            return benchmark.Id;
        }
    }
}
