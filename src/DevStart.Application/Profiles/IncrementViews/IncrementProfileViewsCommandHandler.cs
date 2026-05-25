using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Profiles.IncrementViews
{
    internal sealed class IncrementProfileViewsCommandHandler(
        IApplicationDbContext context,
        ICacheService cache)
        : ICommandHandler<IncrementProfileViewsCommand>
    {
        public async Task<Result> Handle(IncrementProfileViewsCommand command, CancellationToken cancellationToken)
        {
            // Atomic counter bump — avoids a read-modify-write race between concurrent viewers.
            int affected = await context.Profiles
                .Where(p => p.UserId == command.ProfileUserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(p => p.ViewCount, p => p.ViewCount + 1),
                    cancellationToken);

            // The GetProfileById read is cached (5 min); invalidate so the owner's dashboard
            // reflects the new count on the next read instead of serving a stale value.
            if (affected > 0)
            {
                await cache.RemoveAsync(CacheKeys.Profile(command.ProfileUserId), cancellationToken);
            }

            return Result.Success();
        }
    }
}
