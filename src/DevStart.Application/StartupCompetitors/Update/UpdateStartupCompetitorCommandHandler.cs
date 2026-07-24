using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupCompetitors;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupCompetitors.Update
{
    internal sealed class UpdateStartupCompetitorCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService)
        : ICommandHandler<UpdateStartupCompetitorCommand>
    {
        public async Task<Result> Handle(UpdateStartupCompetitorCommand command, CancellationToken cancellationToken)
        {
            StartupCompetitor? competitor = await context.StartupCompetitors
                .SingleOrDefaultAsync(c => c.Id == command.CompetitorId, cancellationToken);

            if (competitor is null)
            {
                return Result.Failure(StartupCompetitorErrors.NotFound(command.CompetitorId));
            }

            StartupMember? startupMember = await context.StartupMembers
                .SingleOrDefaultAsync(
                    sm => sm.StartupId == competitor.StartupId && sm.ProfileId == userContext.UserId,
                    cancellationToken);

            if (startupMember is null || startupMember.Role == StartupRole.Member)
            {
                return Result.Failure(StartupCompetitorErrors.Unauthorized);
            }

            string? domain = StartupCompetitor.NormalizeDomain(command.Website);
            if (domain is null)
            {
                return Result.Failure(StartupCompetitorErrors.InvalidWebsite);
            }

            if (await context.StartupCompetitors.AnyAsync(
                    c => c.StartupId == competitor.StartupId
                        && c.Id != competitor.Id
                        && c.NormalizedDomain == domain,
                    cancellationToken))
            {
                return Result.Failure(StartupCompetitorErrors.DuplicateDomain);
            }

            competitor.Update(
                command.Name,
                command.Website,
                command.Description,
                command.StrengthsVsUs,
                command.WeaknessesVsUs,
                dateTimeProvider.UtcNow);

            await context.SaveChangesAsync(cancellationToken);

            // An edit changes how well the card is documented, which changes the competition factor —
            // the cached score has to go, just as it does on create and delete.
            await cacheService.RemoveAsync(CacheKeys.StartupScore(competitor.StartupId), cancellationToken);

            return Result.Success();
        }
    }
}
