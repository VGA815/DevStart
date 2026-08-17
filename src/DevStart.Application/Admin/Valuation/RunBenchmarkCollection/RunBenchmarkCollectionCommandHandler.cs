using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.BackgroundJobs;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Admin;
using DevStart.SharedKernel;

namespace DevStart.Application.Admin.Valuation.RunBenchmarkCollection
{
    internal sealed class RunBenchmarkCollectionCommandHandler(
        IApplicationDbContext context,
        IBackgroundJobScheduler scheduler,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<RunBenchmarkCollectionCommand>
    {
        public async Task<Result> Handle(
            RunBenchmarkCollectionCommand command,
            CancellationToken cancellationToken)
        {
            if (command.Kind is BenchmarkCollectionKind.MarketCap or BenchmarkCollectionKind.Both)
            {
                scheduler.EnqueueMarketCapCollection();
            }

            if (command.Kind is BenchmarkCollectionKind.Revenue or BenchmarkCollectionKind.Both)
            {
                scheduler.EnqueueRevenueCollection();
            }

            DateTime now = dateTimeProvider.UtcNow;
            context.AdminActionLogs.Add(AdminActionLog.Create(
                userContext.UserId,
                AdminActionType.RunBenchmarkCollection,
                AdminTargetType.BenchmarkDataset,
                // A collection run targets no single row; the reason line carries which collector ran.
                Guid.Empty,
                $"Queued benchmark collection: {command.Kind}",
                now));

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
