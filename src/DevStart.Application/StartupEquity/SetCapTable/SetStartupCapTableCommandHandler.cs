using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups;
using DevStart.Domain.StartupEquity;
using DevStart.Domain.StartupMembers;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupEquity.SetCapTable
{
    internal sealed class SetStartupCapTableCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IStartupAuthorizationService authorizationService,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<SetStartupCapTableCommand>
    {
        public async Task<Result> Handle(SetStartupCapTableCommand command, CancellationToken cancellationToken)
        {
            if (!await authorizationService.IsFounderOrAdminAsync(userContext.UserId, command.StartupId, cancellationToken))
            {
                return Result.Failure(StartupEquityErrors.Unauthorized);
            }

            bool startupExists = await context.Startups
                .AsNoTracking()
                .AnyAsync(s => s.Id == command.StartupId, cancellationToken);
            if (!startupExists)
            {
                return Result.Failure(StartupEquityErrors.StartupNotFound(command.StartupId));
            }

            // Every founder row must point at an actual founder member of this startup.
            HashSet<Guid> founderProfileIds = await context.StartupMembers
                .AsNoTracking()
                .Where(sm => sm.StartupId == command.StartupId && sm.Role == StartupRole.Founder)
                .Select(sm => sm.ProfileId)
                .ToHashSetAsync(cancellationToken);

            foreach (CapTableHolderInput holder in command.Holders)
            {
                if (holder.HolderType == EquityHolderType.Founder
                    && (holder.ProfileId is not { } profileId || !founderProfileIds.Contains(profileId)))
                {
                    return Result.Failure(StartupEquityErrors.FounderNotAMember);
                }
            }

            // Replace the whole table atomically: drop existing rows, insert the new set.
            List<StartupEquityHolder> existing = await context.StartupEquityHolders
                .Where(h => h.StartupId == command.StartupId)
                .ToListAsync(cancellationToken);
            context.StartupEquityHolders.RemoveRange(existing);

            DateTime utcNow = dateTimeProvider.UtcNow;
            StartupEquityHolder? first = null;
            foreach (CapTableHolderInput holder in command.Holders)
            {
                bool isFounder = holder.HolderType == EquityHolderType.Founder;
                StartupEquityHolder entity = StartupEquityHolder.Create(
                    command.StartupId,
                    holder.HolderType,
                    isFounder ? holder.ProfileId : null,
                    isFounder ? null : holder.Name,
                    holder.EquityPercentage,
                    holder.VestingStartDate,
                    holder.VestingMonths,
                    holder.CliffMonths,
                    utcNow);

                first ??= entity;
                context.StartupEquityHolders.Add(entity);
            }

            // Signal derived caches (e.g. the startup score) to refresh. Raised on a tracked new row
            // so it is dispatched after SaveChanges.
            first!.Raise(new StartupCapTableChangedDomainEvent(command.StartupId));

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
