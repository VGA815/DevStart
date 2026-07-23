using DevStart.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.CommunityStandards
{
    /// <summary>
    /// The community pages are anonymous reads, so each of them has to re-apply the public-visibility
    /// rule rather than lean on a permission. Honours lazy ban expiry the same way
    /// <c>GetStartupByIdQueryHandler</c> does: a temporary ban whose expiry has passed is already lifted,
    /// without waiting for the hourly ban-expiry job.
    /// </summary>
    internal static class PublicStartupVisibility
    {
        public static Task<bool> IsVisibleAsync(
            IApplicationDbContext context,
            Guid startupId,
            DateTime utcNow,
            CancellationToken cancellationToken)
            => context.Startups
                .AsNoTracking()
                .AnyAsync(
                    s => s.Id == startupId
                      && !(s.IsBanned && (s.BanExpiresAt == null || s.BanExpiresAt > utcNow)),
                    cancellationToken);
    }
}
