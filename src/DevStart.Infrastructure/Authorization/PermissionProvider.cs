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
                Permissions.StartupsScoreRead,

                Permissions.StartupMembersCreate,
                Permissions.StartupMembersDelete,
                Permissions.StartupMembersChangeRole,
                Permissions.StartupMembersChangeVisibility,
                Permissions.StartupMembersUpdateProfile,

                Permissions.StartupCompetitorsCreate,
                Permissions.StartupCompetitorsUpdate,
                Permissions.StartupCompetitorsDelete,
                Permissions.StartupCompetitorsRead,

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

                Permissions.MessagesSend,
                Permissions.MessagesRead,
                Permissions.MessagesUpdate,

                Permissions.InvestorProfilesCreate,
                Permissions.InvestorProfilesUpdate,
                Permissions.InvestorProfilesRead,

                Permissions.InvestmentApplicationsCreate,
                Permissions.InvestmentApplicationsRespond,
                Permissions.InvestmentApplicationsWithdraw,
                Permissions.InvestmentApplicationsRead,

                Permissions.InvestmentDealsConfirm,
                Permissions.InvestmentDealsRead,

                Permissions.DealDocumentsRead,

                Permissions.SubscriptionsCheckout,
                Permissions.SubscriptionsRead,

                Permissions.ConsentsRead,
                Permissions.ConsentsRevoke,

                Permissions.ExpertCollaborationRequestsCreate,
                Permissions.ExpertCollaborationRequestsRespond,
                Permissions.ExpertCollaborationRequestsRead,
                Permissions.ExpertCollaborationRequestsWithdraw,

                Permissions.ExpertExperiencesCreate,
                Permissions.ExpertExperiencesUpdate,
                Permissions.ExpertExperiencesDelete,
                Permissions.ExpertExperiencesRead,

                Permissions.ExpertProfilesCreate,
                Permissions.ExpertProfilesUpdate,
                Permissions.ExpertProfilesRead,
            },
            [UserSystemRole.Admin] = new HashSet<string>
            {
                Permissions.StartupsCreate,
                Permissions.StartupsUpdate,
                Permissions.StartupsDelete,
                Permissions.StartupsScoreRead,

                Permissions.StartupMembersCreate,
                Permissions.StartupMembersDelete,
                Permissions.StartupMembersChangeRole,
                Permissions.StartupMembersChangeVisibility,
                Permissions.StartupMembersUpdateProfile,

                Permissions.StartupCompetitorsCreate,
                Permissions.StartupCompetitorsUpdate,
                Permissions.StartupCompetitorsDelete,
                Permissions.StartupCompetitorsRead,

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

                Permissions.MessagesSend,
                Permissions.MessagesRead,
                Permissions.MessagesUpdate,

                Permissions.InvestorProfilesCreate,
                Permissions.InvestorProfilesUpdate,
                Permissions.InvestorProfilesRead,

                Permissions.InvestmentApplicationsCreate,
                Permissions.InvestmentApplicationsRespond,
                Permissions.InvestmentApplicationsWithdraw,
                Permissions.InvestmentApplicationsRead,

                Permissions.InvestmentDealsConfirm,
                Permissions.InvestmentDealsRead,

                Permissions.DealDocumentsRead,

                Permissions.SubscriptionsCheckout,
                Permissions.SubscriptionsRead,

                Permissions.ConsentsRead,
                Permissions.ConsentsRevoke,

                Permissions.PaymentsRefund,

                Permissions.ConsentDocumentsCreate,
                Permissions.ConsentDocumentsActivate,

                Permissions.ExpertCollaborationRequestsCreate,
                Permissions.ExpertCollaborationRequestsRespond,
                Permissions.ExpertCollaborationRequestsRead,
                Permissions.ExpertCollaborationRequestsWithdraw,

                Permissions.ExpertExperiencesCreate,
                Permissions.ExpertExperiencesUpdate,
                Permissions.ExpertExperiencesDelete,
                Permissions.ExpertExperiencesRead,

                Permissions.ExpertProfilesCreate,
                Permissions.ExpertProfilesUpdate,
                Permissions.ExpertProfilesRead,
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
