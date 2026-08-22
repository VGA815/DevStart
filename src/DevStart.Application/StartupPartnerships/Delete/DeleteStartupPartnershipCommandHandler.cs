using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupPartnerships;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupPartnerships.Delete
{
    internal sealed class DeleteStartupPartnershipCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        ICacheService cacheService)
        : ICommandHandler<DeleteStartupPartnershipCommand>
    {
        public async Task<Result> Handle(
            DeleteStartupPartnershipCommand command, CancellationToken cancellationToken)
        {
            StartupPartnership? partnership = await context.StartupPartnerships
                .SingleOrDefaultAsync(p => p.Id == command.PartnershipId, cancellationToken);

            if (partnership is null)
            {
                return Result.Failure(StartupPartnershipErrors.NotFound(command.PartnershipId));
            }

            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(
                    sm => sm.StartupId == partnership.StartupId && sm.ProfileId == userContext.UserId,
                    cancellationToken);

            if (startupMember is null || startupMember.Role == StartupRole.Member)
            {
                return Result.Failure(StartupPartnershipErrors.Unauthorized);
            }

            Guid startupId = partnership.StartupId;

            context.StartupPartnerships.Remove(partnership);

            await context.SaveChangesAsync(cancellationToken);

            await cacheService.RemoveAsync(CacheKeys.StartupScore(startupId), cancellationToken);

            return Result.Success();
        }
    }
}
