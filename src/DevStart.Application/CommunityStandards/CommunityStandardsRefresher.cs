using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.CommunityStandards
{
    internal sealed class CommunityStandardsRefresher(
        IApplicationDbContext context,
        ICommunityStandardsDataProvider dataProvider,
        ICommunityStandardsEvaluator evaluator,
        ICacheService cache,
        IDateTimeProvider dateTimeProvider) : ICommunityStandardsRefresher
    {
        public async Task RefreshAsync(Guid startupId, CancellationToken cancellationToken)
        {
            Result<CommunityStandardsInputs> inputs = await dataProvider.GetInputsAsync(startupId, cancellationToken);
            if (inputs.IsFailure)
            {
                // The startup is gone (deleted mid-sweep, say). Nothing to project.
                return;
            }

            DateTime utcNow = dateTimeProvider.UtcNow;
            CommunityStandardsResult result = evaluator.Evaluate(inputs.Value, utcNow);

            StartupCommunityStandards? projection = await context.StartupCommunityStandards
                .SingleOrDefaultAsync(s => s.StartupId == startupId, cancellationToken);

            if (projection is null)
            {
                context.StartupCommunityStandards.Add(StartupCommunityStandards.Create(
                    startupId, result.CompletedCount, result.TotalCount, result.Level, utcNow));
            }
            else
            {
                projection.Update(result.CompletedCount, result.TotalCount, result.Level, utcNow);
            }

            await context.SaveChangesAsync(cancellationToken);
            await cache.RemoveAsync(CacheKeys.StartupCommunityStandards(startupId), cancellationToken);
        }
    }
}
