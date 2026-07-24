using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupCompetitors;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupCompetitors.Create
{
    internal sealed class CreateStartupCompetitorCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService)
        : ICommandHandler<CreateStartupCompetitorCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateStartupCompetitorCommand command, CancellationToken cancellationToken)
        {
            if (!await context.Startups.AnyAsync(s => s.Id == command.StartupId, cancellationToken))
            {
                return Result.Failure<Guid>(StartupErrors.NotFound(command.StartupId));
            }

            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(
                    sm => sm.StartupId == command.StartupId && sm.ProfileId == userContext.UserId,
                    cancellationToken);

            if (startupMember is null)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            if (startupMember.Role == StartupRole.Member)
            {
                return Result.Failure<Guid>(UserErrors.Unauthorized());
            }

            string? domain = StartupCompetitor.NormalizeDomain(command.Website);
            if (domain is null)
            {
                return Result.Failure<Guid>(StartupCompetitorErrors.InvalidWebsite);
            }

            // Hygiene: the quality-of-analysis half of the competition score would otherwise be
            // farmable by cloning one competitor under several URLs. The unique index on
            // (startup_id, normalized_domain) is the race backstop behind this check.
            if (await context.StartupCompetitors.AnyAsync(
                    c => c.StartupId == command.StartupId && c.NormalizedDomain == domain, cancellationToken))
            {
                return Result.Failure<Guid>(StartupCompetitorErrors.DuplicateDomain);
            }

            int existingCount = await context.StartupCompetitors
                .CountAsync(c => c.StartupId == command.StartupId, cancellationToken);
            if (existingCount >= StartupCompetitor.MaxPerStartup)
            {
                return Result.Failure<Guid>(StartupCompetitorErrors.LimitReached);
            }

            StartupCompetitor competitor = StartupCompetitor.Create(
                command.StartupId,
                command.Name,
                command.Website,
                command.Description,
                command.StrengthsVsUs,
                command.WeaknessesVsUs,
                dateTimeProvider.UtcNow);

            context.StartupCompetitors.Add(competitor);

            await context.SaveChangesAsync(cancellationToken);

            await cacheService.RemoveAsync(CacheKeys.StartupScore(command.StartupId), cancellationToken);

            return competitor.Id;
        }
    }
}
