using Microsoft.EntityFrameworkCore;
using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.InviteTokens;
using DevStart.Domain.Startups;
using DevStart.SharedKernel;
using DevStart.Domain.StartupMembers;

namespace DevStart.Application.InviteTokens.Use
{
    internal sealed class UseInviteTokenCommandHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider, IUserContext userContext)
        : ICommandHandler<UseInviteTokenCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(UseInviteTokenCommand command, CancellationToken cancellationToken)
        {
            InviteToken? inviteToken = await context.InviteTokens.SingleOrDefaultAsync(it => it.Id == command.TokenId, cancellationToken);

            if (inviteToken == null)
            {
                return Result.Failure<Guid>(InviteTokenErrors.NotFound(command.TokenId));
            }
            if (inviteToken.IsUsed)
            {
                return Result.Failure<Guid>(InviteTokenErrors.AlreadyUsed);
            }
            if (inviteToken.ExpiresAt < dateTimeProvider.UtcNow)
            {
                return Result.Failure<Guid>(InviteTokenErrors.Expired);
            }

            Startup? startup = await context.Startups.SingleOrDefaultAsync(s => s.Id == inviteToken.StartupId, cancellationToken);

            if (startup == null)
            {
                return Result.Failure<Guid>(StartupErrors.NotFound(inviteToken.StartupId));
            }
            if (await context.StartupMembers.AnyAsync(ur => ur.ProfileId == userContext.UserId && ur.StartupId == startup.Id, cancellationToken))
            {
                return Result.Failure<Guid>(StartupErrors.UserAlreadyMember);
            }
            StartupMember startupMember = StartupMember.Create(userContext.UserId, startup.Id, StartupRole.Member, true, dateTimeProvider.UtcNow);

            context.StartupMembers.Add(startupMember);
            inviteToken.IsUsed = true;
            await context.SaveChangesAsync(cancellationToken);

            return startup.Id;
        }
    }
}
