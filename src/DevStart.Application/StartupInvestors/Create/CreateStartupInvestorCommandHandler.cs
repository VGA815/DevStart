using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupInvestors;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupInvestors.Create
{
    internal sealed class CreateStartupInvestorCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateStartupInvestorCommand, (Guid startupId, Guid profileId)>
    {
        public async Task<Result<(Guid startupId, Guid profileId)>> Handle(CreateStartupInvestorCommand command, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(sm => sm.Id == command.StartupId, cancellationToken))
            {
                return Result.Failure<(Guid startupId, Guid profileId)>(StartupErrors.NotFound(command.StartupId));
            }
            if (!await context.Users.AnyAsync(u => u.Id == userContext.UserId, cancellationToken))
            {
                return Result.Failure<(Guid startupId, Guid profileId)>(UserErrors.NotFound(userContext.UserId));
            }

            StartupInvestor startupInvestor = new StartupInvestor(
                command.ProfileId,
                command.StartupId,
                command.IsPublic,
                dateTimeProvider.UtcNow,
                dateTimeProvider.UtcNow);

            context.StartupInvestors.Add(startupInvestor);

            await context.SaveChangesAsync(cancellationToken);

            return (startupInvestor.StartupId, startupInvestor.ProfileId);
        }
    }
}
