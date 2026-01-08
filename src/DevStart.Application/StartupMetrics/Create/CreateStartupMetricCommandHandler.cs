using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupMetrics;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMetrics.Create
{
    internal sealed class CreateStartupMetricCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateStartupMetricCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateStartupMetricCommand command, CancellationToken cancellationToken)
        {
            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId, cancellationToken);

            if (startupMember == null)
            {
                return Result.Failure<Guid>(StartupMemberErrors.NotFound(userContext.UserId, command.StartupId));
            }

            StartupMetric startupMetric = new StartupMetric(
                command.StartupId,
                command.MetricType,
                command.Value,
                dateTimeProvider);

            context.StartupMetrics.Add(startupMetric);

            await context.SaveChangesAsync(cancellationToken);

            return startupMetric.Id;
        }
    }
}
