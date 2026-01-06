using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups.Delete
{
    internal sealed class DeleteStartupCommandHandler(IApplicationDbContext context, IUserContext userContext)
        : ICommandHandler<DeleteStartupCommand>
    {
        public async Task<Result> Handle(DeleteStartupCommand command, CancellationToken cancellationToken)
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

            Startup? startup = await context.Startups
                .SingleOrDefaultAsync(s => s.Id == startupMember.StartupId, cancellationToken);

            context.Startups.Remove(startup!);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
