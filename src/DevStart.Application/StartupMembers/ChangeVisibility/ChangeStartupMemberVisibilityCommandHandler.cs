using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMembers.ChangeVisibility
{
    internal sealed class ChangeStartupMemberVisibilityCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<ChangeStartupMemberVisibilityCommand>
    {
        public async Task<Result> Handle(ChangeStartupMemberVisibilityCommand command, CancellationToken cancellationToken)
        {
            if (!userContext.UserId.Equals(command.ProfileId))
            {
                return Result.Failure(UserErrors.Unauthorized());
            }

            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == command.ProfileId, cancellationToken);

            if (startupMember == null)
            {
                return Result.Failure(StartupMemberErrors.NotFound(command.ProfileId, command.StartupId));
            }

            startupMember.IsPublic = command.IsPublic;
            startupMember.UpdatedAt = dateTimeProvider.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
