using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMembers.ChangeRole
{
    internal sealed class ChangeStartupMemberRoleCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<ChangeStartupMemberRoleCommand>
    {
        public async Task<Result> Handle(ChangeStartupMemberRoleCommand command, CancellationToken cancellationToken)
        {
            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId, cancellationToken);

            if (startupMember == null)
            {
                return Result.Failure(StartupMemberErrors.NotFound(userContext.UserId, command.StartupId));
            }

            if (startupMember.Role != StartupRole.Founder)
            {
                return Result.Failure(UserErrors.Unauthorized());
            }

            StartupMember? startupMember1 = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == command.ProfileId, cancellationToken);

            if (startupMember1 == null)
            {
                return Result.Failure(StartupMemberErrors.NotFound(command.ProfileId, command.StartupId));
            }
            if (startupMember.Role == startupMember1.Role && startupMember1.Role == StartupRole.Founder)
            {
                if (startupMember1.ProfileId != startupMember.ProfileId)
                {
                    return Result.Failure(UserErrors.Unauthorized());
                }
            }
            if (startupMember.StartupId != startupMember1.StartupId)
            {
                return Result.Failure(UserErrors.Unauthorized());
            }

            startupMember1.UpdatedAt = dateTimeProvider.UtcNow;
            startupMember1.Role = command.Role;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
