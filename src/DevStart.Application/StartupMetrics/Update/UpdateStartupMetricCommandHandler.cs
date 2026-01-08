using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupMetrics;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMetrics.Update
{
    internal sealed class UpdateStartupMetricCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<UpdateStartupMetricCommand>
    {
        public async Task<Result> Handle(UpdateStartupMetricCommand command, CancellationToken cancellationToken)
        {
            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId  == command.StartupId && sm.ProfileId == userContext.UserId, cancellationToken);

            if (startupMember == null)
            {
                return Result.Failure(StartupMemberErrors.NotFound(userContext.UserId, command.StartupId));
            }

            StartupMetric? startupMetric = await context.StartupMetrics
                .SingleOrDefaultAsync(sm => sm.Id == command.Id, cancellationToken);

            if (startupMetric is null)
            {
                return Result.Failure(StartupMetricErrors.NotFound(command.Id));
            }

            startupMetric.MetricType = command.MetricType;
            startupMetric.Value = command.Value;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
