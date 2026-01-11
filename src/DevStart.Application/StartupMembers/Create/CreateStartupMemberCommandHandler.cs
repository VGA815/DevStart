using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupMembers.Create
{
    internal sealed class CreateStartupMemberCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateStartupMemberCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateStartupMemberCommand command, CancellationToken cancellationToken)
        {
            StartupMember? startupMemberAdder = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId, cancellationToken);

            if (startupMemberAdder == null)
            {
                return Result.Failure<Guid>(StartupMemberErrors.NotFound(userContext.UserId, command.StartupId));
            }

            if (startupMemberAdder.Role == StartupRole.Member)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            if (await context.StartupMembers.AnyAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == command.ProfileId))
            {
                return Result.Failure<Guid>(StartupMemberErrors.IsNotUnique);
            }

            if (startupMemberAdder.StartupId != command.StartupId)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            Startup? startup = await context.Startups
                .SingleOrDefaultAsync(s => s.Id == command.StartupId, cancellationToken);

            if (startup == null)
            {
                return Result.Failure<Guid>(StartupErrors.NotFound(command.StartupId));
            }

            StartupMember startupMember = new StartupMember(command.ProfileId, command.StartupId, command.Role, command.IsPublic, dateTimeProvider.UtcNow, dateTimeProvider.UtcNow);

            context.StartupMembers.Add(startupMember);

            await context.SaveChangesAsync(cancellationToken);

            return startupMember.ProfileId;
        }
    }
}
