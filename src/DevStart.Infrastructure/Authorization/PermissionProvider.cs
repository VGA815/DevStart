using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Data;
using DevStart.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Infrastructure.Authorization
{
    internal sealed class PermissionProvider(IApplicationDbContext context)
    {
        private static readonly Dictionary<UserSystemRole, HashSet<string>> RolePermissions = new()
        {
            [UserSystemRole.User] = new HashSet<string>
            {
                Permissions.StartupsCreate,
                Permissions.StartupsUpdate,
                Permissions.StartupsDelete,

                Permissions.StartupMembersCreate,
                Permissions.StartupMembersDelete,
                Permissions.StartupMembersChangeRole,
                Permissions.StartupMembersChangeVisibility,

                Permissions.StartupFollowersCreate,
                Permissions.StartupFollowersDelete,

                Permissions.StartupInvestorsCreate,
                Permissions.StartupInvestorsChangeVisibility,

                Permissions.StartupMetricsCreate,
                Permissions.StartupMetricsUpdate,
                Permissions.StartupMetricsDelete,

                Permissions.StartupRoadmapItemsCreate,
                Permissions.StartupRoadmapItemsUpdate,
                Permissions.StartupRoadmapItemsDelete,

                Permissions.StartupProductsUpdate,

                Permissions.ProfilesCreate,
                Permissions.ProfilesUpdate,
                Permissions.ProfilesDelete,

                Permissions.MediaFilesUpload,
                Permissions.MediaFilesDelete,

                Permissions.UsersRead,

                Permissions.NotificationsRead,
                Permissions.NotificationsUpdate,

                Permissions.UserPreferencesRead,
                Permissions.UserPreferencesUpdate,
            },
            [UserSystemRole.Admin] = new HashSet<string>
            {
                Permissions.StartupsCreate,
                Permissions.StartupsUpdate,
                Permissions.StartupsDelete,

                Permissions.StartupMembersCreate,
                Permissions.StartupMembersDelete,
                Permissions.StartupMembersChangeRole,
                Permissions.StartupMembersChangeVisibility,

                Permissions.StartupFollowersCreate,
                Permissions.StartupFollowersDelete,

                Permissions.StartupInvestorsCreate,
                Permissions.StartupInvestorsChangeVisibility,

                Permissions.StartupMetricsCreate,
                Permissions.StartupMetricsUpdate,
                Permissions.StartupMetricsDelete,

                Permissions.StartupRoadmapItemsCreate,
                Permissions.StartupRoadmapItemsUpdate,
                Permissions.StartupRoadmapItemsDelete,

                Permissions.StartupProductsUpdate,

                Permissions.ProfilesCreate,
                Permissions.ProfilesUpdate,
                Permissions.ProfilesDelete,

                Permissions.MediaFilesUpload,
                Permissions.MediaFilesDelete,

                Permissions.UsersRead,

                Permissions.NotificationsRead,
                Permissions.NotificationsUpdate,

                Permissions.UserPreferencesRead,
                Permissions.UserPreferencesUpdate,
            },
        };

        public async Task<HashSet<string>> GetForUserIdAsync(Guid userId)
        {
            UserSystemRole? role = await context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => (UserSystemRole?)u.Role)
                .SingleOrDefaultAsync();

            return role is not null && RolePermissions.TryGetValue(role.Value, out HashSet<string>? permissions)
                ? permissions
                : [];
        }
    }
}
