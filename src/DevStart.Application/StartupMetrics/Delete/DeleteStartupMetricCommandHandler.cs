using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupMetrics;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMetrics.Delete
{
    internal sealed class DeleteStartupMetricCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ICacheService cacheService)
        : ICommandHandler<DeleteStartupMetricCommand>
    {
        public async Task<Result> Handle(DeleteStartupMetricCommand command, CancellationToken cancellationToken)
        {
            StartupMetric? startupMetric = await context.StartupMetrics
                .SingleOrDefaultAsync(sm => sm.Id == command.MetricId, cancellationToken);

            if (startupMetric == null)
            {
                return Result.Failure(StartupMetricErrors.NotFound(command.MetricId));
            }

            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == startupMetric.StartupId && sm.ProfileId == userContext.UserId, cancellationToken);

            if (startupMember == null)
            {
                return Result.Failure(StartupMemberErrors.NotFound(userContext.UserId, startupMetric.StartupId));
            }

            if (startupMember.Role == StartupRole.Member)
            {
                return Result.Failure(UserErrors.Unauthorized());
            }

            Guid startupId = startupMetric.StartupId;

            context.StartupMetrics.Remove(startupMetric);

            await context.SaveChangesAsync(cancellationToken);

            await cacheService.RemoveAsync(CacheKeys.StartupScore(startupId), cancellationToken);

            return Result.Success();
        }
    }
}
