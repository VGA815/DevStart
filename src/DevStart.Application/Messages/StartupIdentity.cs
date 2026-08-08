using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Messages;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Messages
{
    /// <summary>
    /// Single place answering "may this user act as this startup in chat?" — see
    /// <see cref="MessagingRoles"/> for the rule itself.
    /// </summary>
    internal static class StartupIdentity
    {
        public static Task<bool> CanActAsAsync(
            IApplicationDbContext context,
            Guid startupId,
            Guid userId,
            CancellationToken cancellationToken) =>
            context.StartupMembers.AnyAsync(
                sm => sm.StartupId == startupId
                   && sm.ProfileId == userId
                   && MessagingRoles.CanActAsStartup.Contains(sm.Role),
                cancellationToken);

        /// <summary>Startups the user speaks for, used to resolve their side of a conversation.</summary>
        public static Task<List<Guid>> ActableStartupIdsAsync(
            IApplicationDbContext context,
            Guid userId,
            CancellationToken cancellationToken) =>
            context.StartupMembers
                .Where(sm => sm.ProfileId == userId && MessagingRoles.CanActAsStartup.Contains(sm.Role))
                .Select(sm => sm.StartupId)
                .ToListAsync(cancellationToken);
    }
}
