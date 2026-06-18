using DevStart.Application.Abstractions.Data;
using DevStart.Domain.StartupMembers;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Startups
{
    internal sealed class StartupAuthorizationService(IApplicationDbContext context) : IStartupAuthorizationService
    {
        public Task<bool> IsFounderOrAdminAsync(Guid userId, Guid startupId, CancellationToken cancellationToken)
            => context.StartupMembers
                .AsNoTracking()
                .AnyAsync(
                    sm => sm.StartupId == startupId
                       && sm.ProfileId == userId
                       && (sm.Role == StartupRole.Founder || sm.Role == StartupRole.Administration),
                    cancellationToken);
    }
}
