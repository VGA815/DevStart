using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups.Update
{
    internal sealed class UpdateStartupCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<UpdateStartupCommand>
    {
        public async Task<Result> Handle(UpdateStartupCommand command, CancellationToken cancellationToken)
        {
            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId, cancellationToken);
            if (startupMember == null)
            {
                return Result.Failure(StartupMemberErrors.NotFound(userContext.UserId, command.StartupId));
            }
            if (startupMember.Role == StartupRole.Member)
            {
                return Result.Failure(UserErrors.Unauthorized());
            }

            Startup? startup = await context.Startups
                .SingleOrDefaultAsync(s => s.Id == command.StartupId, cancellationToken);
            
            // TODO: Email verification

            startup!.Url = command.Url;
            startup.AvatarUrl = command.AvatarUrl;
            startup.SocialMediaLinks = command.SocialMediaLinks;
            startup.BillingEmail = command.BillingEmail;
            startup.PublicEmail = command.PublicEmail;
            startup.Description = command.Description;
            startup.Location = command.Location;
            startup.Name = command.Name;
            startup.IsStopped = command.IsStopped;
            startup.Stage = command.Stage;
            startup.UpdatedAt = dateTimeProvider.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
