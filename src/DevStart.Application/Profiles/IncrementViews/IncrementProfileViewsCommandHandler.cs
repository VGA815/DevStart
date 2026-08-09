using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Profiles.IncrementViews
{
    internal sealed class IncrementProfileViewsCommandHandler(IApplicationDbContext context)
        : ICommandHandler<IncrementProfileViewsCommand>
    {
        public async Task<Result> Handle(IncrementProfileViewsCommand command, CancellationToken cancellationToken)
        {
            // Atomic counter bump — avoids a read-modify-write race between concurrent viewers.
            await context.Profiles
                .Where(p => p.UserId == command.ProfileUserId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(p => p.ViewCount, p => p.ViewCount + 1),
                    cancellationToken);

            // Deliberately no cache eviction here. This command runs on every non-owner GET of the
            // profile, and GetProfileById is cached under the same key — evicting would delete the
            // entry the read just populated, leaving the cache with a ~0% hit rate and adding a
            // Redis round trip to a hot anonymous path. ViewCount is a vanity counter, so letting
            // the 5-minute TTL carry it is the right trade: the displayed count can trail the true
            // one by up to that long.
            return Result.Success();
        }
    }
}
