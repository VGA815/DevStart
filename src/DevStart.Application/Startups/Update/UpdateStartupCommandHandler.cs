using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups.Update
{
    internal sealed class UpdateStartupCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService)
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
            startup.ShortDescription = command.ShortDescription;
            startup.AvatarId = command.AvatarId;
            startup.SocialMediaLinks = command.SocialMediaLinks;
            startup.BillingEmail = command.BillingEmail;
            startup.PublicEmail = command.PublicEmail;
            startup.Description = command.Description;
            startup.Location = command.Location;
            startup.Name = command.Name;
            startup.IsStopped = command.IsStopped;
            startup.Stage = command.Stage;
            startup.Tam = command.Tam;
            startup.Sam = command.Sam;
            startup.Som = command.Som;
            startup.MarketGrowthRate = command.MarketGrowthRate;
            startup.HasPatents = command.HasPatents;
            startup.UpdatedAt = dateTimeProvider.UtcNow;

            startup.Raise(new StartupUpdatedDomainEvent(startup.Id));

            await context.SaveChangesAsync(cancellationToken);

            await cacheService.RemoveAsync(CacheKeys.StartupScore(startup.Id), cancellationToken);

            return Result.Success();
        }
    }
}
