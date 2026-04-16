using Microsoft.EntityFrameworkCore;
using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InviteTokens;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Users;

namespace DevStart.Application.InviteTokens.Create
{
    internal sealed class CreateInviteCommandHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider, IUserContext userContext)
        : ICommandHandler<CreateInviteTokenCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateInviteTokenCommand command, CancellationToken cancellationToken)
        {
            StartupMember? member = await context.StartupMembers
                .FirstOrDefaultAsync(m => m.StartupId == command.StartupId && m.ProfileId == userContext.UserId, cancellationToken);

            if (member is null)
            {
                return Result.Failure<Guid>(StartupMemberErrors.NotFound(userContext.UserId, command.StartupId));
            }
            if (member.Role != StartupRole.Administration && member.Role != StartupRole.Founder)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            InviteToken inviteToken = InviteToken.Create(command.StartupId, dateTimeProvider.UtcNow.AddDays(1));

            context.InviteTokens.Add(inviteToken);
            await context.SaveChangesAsync(cancellationToken);

            return inviteToken.Id;
        }
    }
}
