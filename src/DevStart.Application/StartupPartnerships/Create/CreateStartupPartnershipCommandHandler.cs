using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupPartnerships;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupPartnerships.Create
{
    internal sealed class CreateStartupPartnershipCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService)
        : ICommandHandler<CreateStartupPartnershipCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(
            CreateStartupPartnershipCommand command, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(s => s.Id == command.StartupId, cancellationToken))
            {
                return Result.Failure<Guid>(StartupErrors.NotFound(command.StartupId));
            }

            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(
                    sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId,
                    cancellationToken);

            if (startupMember is null || startupMember.Role == StartupRole.Member)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            string? domain = StartupPartnership.NormalizeDomain(command.Website);
            if (domain is null)
            {
                return Result.Failure<Guid>(StartupPartnershipErrors.InvalidWebsite);
            }

            // Hygiene, by the competitor-card precedent: one record per partner domain per startup.
            // Without it the Berkus ceiling would be reachable by listing the same partner three times
            // under three URLs. The unique index is the race backstop behind this check.
            if (await context.StartupPartnerships.AnyAsync(
                    p => p.StartupId == command.StartupId && p.NormalizedDomain == domain, cancellationToken))
            {
                return Result.Failure<Guid>(StartupPartnershipErrors.DuplicateDomain);
            }

            int existingCount = await context.StartupPartnerships
                .CountAsync(p => p.StartupId == command.StartupId, cancellationToken);
            if (existingCount >= StartupPartnership.MaxPerStartup)
            {
                return Result.Failure<Guid>(StartupPartnershipErrors.LimitReached);
            }

            StartupPartnership partnership = StartupPartnership.Create(
                command.StartupId,
                command.PartnerName,
                command.Website,
                domain,
                command.Kind,
                command.Description,
                dateTimeProvider.UtcNow);

            context.StartupPartnerships.Add(partnership);

            await context.SaveChangesAsync(cancellationToken);

            // A worked-out record moves the Berkus partnerships factor, so the cached score — which
            // carries the valuation — has to go.
            await cacheService.RemoveAsync(CacheKeys.StartupScore(command.StartupId), cancellationToken);

            return partnership.Id;
        }
    }
}
