using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMembers.Delete
{
    internal sealed class DeleteStartupMemberCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<DeleteStartupMemberCommand>
    {
        public async Task<Result> Handle(DeleteStartupMemberCommand command, CancellationToken cancellationToken)
        {
            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == command.ProfileId, cancellationToken);

            if (startupMember == null)
            {
                return Result.Failure(StartupMemberErrors.NotFound(command.ProfileId, command.StartupId));
            }

            StartupMember? startupMember1 = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId, cancellationToken);

            if (startupMember1 == null)
            {
                return Result.Failure(StartupMemberErrors.NotFound(userContext.UserId, command.StartupId));
            }

            if (startupMember1.StartupId !=  startupMember.StartupId)
            {
                return Result.Failure(UserErrors.Unauthorized());
            }

            if (startupMember1.Role != StartupRole.Founder || startupMember1.ProfileId != startupMember.ProfileId)
            {
                return Result.Failure(UserErrors.Unauthorized());
            }

            context.StartupMembers.Remove(startupMember);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
