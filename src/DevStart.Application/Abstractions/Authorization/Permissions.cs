namespace DevStart.Application.Abstractions.Authorization
{
    public static class Permissions
    {
        public const string StartupsCreate                   = "startups::create";
        public const string StartupsUpdate                   = "startups::update";
        public const string StartupsDelete                   = "startups::delete";

        public const string StartupMembersCreate             = "startup_members::create";
        public const string StartupMembersDelete             = "startup_members::delete";
        public const string StartupMembersChangeRole         = "startup_members::change_role";
        public const string StartupMembersChangeVisibility   = "startup_members::change_visibility";

        public const string StartupFollowersCreate           = "startup_followers::create";
        public const string StartupFollowersDelete           = "startup_followers::delete";

        public const string StartupInvestorsCreate           = "startup_investors::create";
        public const string StartupInvestorsChangeVisibility = "startup_investors::change_visibility";

        public const string StartupMetricsCreate             = "startup_metrics::create";
        public const string StartupMetricsUpdate             = "startup_metrics::update";
        public const string StartupMetricsDelete             = "startup_metrics::delete";

        public const string StartupRoadmapItemsCreate        = "startup_roadmap_items::create";
        public const string StartupRoadmapItemsUpdate        = "startup_roadmap_items::update";
        public const string StartupRoadmapItemsDelete        = "startup_roadmap_items::delete";

        public const string StartupProductsUpdate            = "startup_products::update";

        public const string ProfilesCreate                   = "profiles::create";
        public const string ProfilesUpdate                   = "profiles::update";
        public const string ProfilesDelete                   = "profiles::delete";

        public const string MediaFilesUpload                 = "media_files::upload";
        public const string MediaFilesDelete                 = "media_files::delete";

        public const string UsersRead                        = "users::read";

        public const string NotificationsRead                = "notifications::read";
        public const string NotificationsUpdate              = "notifications::update";

        public const string UserPreferencesRead              = "user_preferences::read";
        public const string UserPreferencesUpdate            = "user_preferences::update";
    }
}
