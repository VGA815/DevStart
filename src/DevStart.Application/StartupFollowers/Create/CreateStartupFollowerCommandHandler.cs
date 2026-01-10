using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupFollowers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupFollowers.Create
{
    internal sealed class CreateStartupFollowerCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateStartupFollowerCommand, (Guid startupId, Guid profileId)>
    {
        public async Task<Result<(Guid startupId, Guid profileId)>> Handle(CreateStartupFollowerCommand command, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(s => s.Id == command.StartupId, cancellationToken))
            {
                return Result.Failure<(Guid startupId, Guid profileId)>(StartupErrors.NotFound(command.StartupId));
            }
            if (userContext.UserId != command.ProfileId)
            {
                return Result.Failure<(Guid startupId, Guid profileId)>(UserErrors.Unauthorized());
            }

            StartupFollower startupFollower = new StartupFollower(
                command.ProfileId,
                command.StartupId,
                dateTimeProvider);

            context.StartupFollowers.Add(startupFollower);

            await context.SaveChangesAsync(cancellationToken);

            return (startupFollower.StartupId, startupFollower.ProfileId);
        }
    }
}
