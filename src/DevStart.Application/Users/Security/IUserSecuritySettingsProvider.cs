using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Security;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.Security
{
    /// <summary>
    /// Reads a user's security policy. A missing row means defaults, which is why every existing user
    /// works without a backfill — the row only materializes when they first change something.
    /// </summary>
    public interface IUserSecuritySettingsProvider
    {
        /// <summary>
        /// The user's settings, or a detached default instance when they have never saved any.
        /// The returned entity is not added to the change tracker.
        /// </summary>
        Task<UserSecuritySettings> GetOrDefaultAsync(Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// The user's settings as a tracked entity, inserting a defaults row if none exists. The
        /// caller owns the SaveChanges.
        /// </summary>
        Task<UserSecuritySettings> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken);
    }

    internal sealed class UserSecuritySettingsProvider(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider) : IUserSecuritySettingsProvider
    {
        public async Task<UserSecuritySettings> GetOrDefaultAsync(Guid userId, CancellationToken cancellationToken)
        {
            UserSecuritySettings? existing = await context.UserSecuritySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            return existing ?? UserSecuritySettings.CreateDefault(userId, dateTimeProvider.UtcNow);
        }

        public async Task<UserSecuritySettings> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken)
        {
            UserSecuritySettings? existing = await context.UserSecuritySettings
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            if (existing is not null)
            {
                return existing;
            }

            UserSecuritySettings created = UserSecuritySettings.CreateDefault(userId, dateTimeProvider.UtcNow);
            context.UserSecuritySettings.Add(created);
            return created;
        }
    }
}
