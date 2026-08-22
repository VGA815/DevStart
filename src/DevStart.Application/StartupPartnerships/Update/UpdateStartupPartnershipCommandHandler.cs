using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupPartnerships;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupPartnerships.Update
{
    internal sealed class UpdateStartupPartnershipCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService)
        : ICommandHandler<UpdateStartupPartnershipCommand>
    {
        public async Task<Result> Handle(
            UpdateStartupPartnershipCommand command, CancellationToken cancellationToken)
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

            string? domain = StartupPartnership.NormalizeDomain(command.Website);
            if (domain is null)
            {
                return Result.Failure(StartupPartnershipErrors.InvalidWebsite);
            }

            if (await context.StartupPartnerships.AnyAsync(
                    p => p.StartupId == partnership.StartupId
                        && p.Id != partnership.Id
                        && p.NormalizedDomain == domain,
                    cancellationToken))
            {
                return Result.Failure(StartupPartnershipErrors.DuplicateDomain);
            }

            partnership.Update(
                command.PartnerName,
                command.Website,
                domain,
                command.Kind,
                command.Description,
                dateTimeProvider.UtcNow);

            await context.SaveChangesAsync(cancellationToken);

            // An edit can add or remove the description, which is exactly what makes the record count
            // towards the Berkus factor — so the cached score goes, as it does on create and delete.
            await cacheService.RemoveAsync(CacheKeys.StartupScore(partnership.StartupId), cancellationToken);

            return Result.Success();
        }
    }
}
