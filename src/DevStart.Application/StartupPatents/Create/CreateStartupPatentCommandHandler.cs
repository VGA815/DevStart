using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.StartupPatents;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.StartupPatents.Create
{
    internal sealed class CreateStartupPatentCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider,
        ICacheService cacheService)
        : ICommandHandler<CreateStartupPatentCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateStartupPatentCommand command, CancellationToken cancellationToken)
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

            DateTime now = dateTimeProvider.UtcNow;

            string? normalized = StartupPatent.NormalizeNumber(command.Number);
            if (!StartupPatent.IsNumberWellFormed(command.Kind, normalized, now.Year))
            {
                return Result.Failure<Guid>(StartupPatentErrors.InvalidNumber(command.Kind));
            }

            // Hygiene, by the competitor-card precedent: one record per (kind, number) per startup. The
            // unique index ux_startup_patents_startup_kind_number is the race backstop behind this.
            if (await context.StartupPatents.AnyAsync(
                    p => p.StartupId == command.StartupId
                        && p.Kind == command.Kind
                        && p.NumberNormalized == normalized,
                    cancellationToken))
            {
                return Result.Failure<Guid>(StartupPatentErrors.DuplicateNumber);
            }

            int existingCount = await context.StartupPatents
                .CountAsync(p => p.StartupId == command.StartupId, cancellationToken);
            if (existingCount >= StartupPatent.MaxPerStartup)
            {
                return Result.Failure<Guid>(StartupPatentErrors.LimitReached);
            }

            StartupPatent patent = StartupPatent.Create(
                command.StartupId, command.Kind, command.Number, normalized!, now);

            context.StartupPatents.Add(patent);

            await context.SaveChangesAsync(cancellationToken);

            await cacheService.RemoveAsync(CacheKeys.StartupPatents(command.StartupId), cancellationToken);

            // The score itself cannot move (SC-65 pins that down with tests), but the provenance chip on
            // the Product factor can — a record that matches the register under the declared ИНН lights
            // "сверено с реестром". Dropping the cached score keeps the chip honest right away.
            await cacheService.RemoveAsync(CacheKeys.StartupScore(command.StartupId), cancellationToken);

            return patent.Id;
        }
    }
}
