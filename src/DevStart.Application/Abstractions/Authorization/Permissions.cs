namespace DevStart.Application.Abstractions.Authorization
{
    public static class Permissions
    {
        public const string StartupsCreate                   = "startups::create";
        public const string StartupsUpdate                   = "startups::update";
        public const string StartupsDelete                   = "startups::delete";
        public const string StartupsScoreRead                = "startups::score_read";

        public const string StartupMembersCreate             = "startup_members::create";
        public const string StartupMembersDelete             = "startup_members::delete";
        public const string StartupMembersChangeRole         = "startup_members::change_role";
        public const string StartupMembersChangeVisibility   = "startup_members::change_visibility";
        public const string StartupMembersUpdateProfile      = "startup_members::update_profile";

        public const string StartupEquityRead                = "startup_equity::read";
        public const string StartupEquityManage              = "startup_equity::manage";

        public const string StartupCompetitorsCreate         = "startup_competitors::create";
        public const string StartupCompetitorsUpdate         = "startup_competitors::update";
        public const string StartupCompetitorsDelete         = "startup_competitors::delete";
        public const string StartupCompetitorsRead           = "startup_competitors::read";

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

        public const string CommunityDocumentsManage         = "community_documents::manage";

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

        public const string MessagesSend                     = "messages::send";
        public const string MessagesRead                     = "messages::read";
        public const string MessagesUpdate                   = "messages::update";

        public const string InvestorProfilesCreate           = "investor_profiles::create";
        public const string InvestorProfilesUpdate           = "investor_profiles::update";
        public const string InvestorProfilesRead             = "investor_profiles::read";

        public const string ExpertProfilesCreate             = "expert_profiles::create";
        public const string ExpertProfilesUpdate             = "expert_profiles::update";
        public const string ExpertProfilesRead               = "expert_profiles::read";

        public const string ExpertExperiencesCreate          = "expert_experiences::create";
        public const string ExpertExperiencesUpdate          = "expert_experiences::update";
        public const string ExpertExperiencesDelete          = "expert_experiences::delete";
        public const string ExpertExperiencesRead            = "expert_experiences::read";

        public const string ExpertCollaborationRequestsCreate   = "expert_collaboration_requests::create";
        public const string ExpertCollaborationRequestsRespond  = "expert_collaboration_requests::respond";
        public const string ExpertCollaborationRequestsWithdraw = "expert_collaboration_requests::withdraw";
        public const string ExpertCollaborationRequestsRead     = "expert_collaboration_requests::read";

        public const string InvestmentApplicationsCreate     = "investment_applications::create";
        public const string InvestmentApplicationsRespond    = "investment_applications::respond";
        public const string InvestmentApplicationsWithdraw   = "investment_applications::withdraw";
        public const string InvestmentApplicationsRead       = "investment_applications::read";

        public const string InvestmentDealsConfirm           = "investment_deals::confirm";
        public const string InvestmentDealsRead              = "investment_deals::read";

        public const string DealDocumentsRead                = "deal_documents::read";

        public const string SubscriptionsCheckout            = "subscriptions::checkout";
        public const string SubscriptionsRead                = "subscriptions::read";

        public const string ServiceOrdersCheckout            = "service_orders::checkout";

        public const string PaymentsRefund                   = "payments::refund";

        public const string ConsentsRead                     = "consents::read";
        public const string ConsentsRevoke                   = "consents::revoke";

        public const string ConsentDocumentsCreate           = "consent_documents::create";
        public const string ConsentDocumentsActivate         = "consent_documents::activate";

        public const string AdminUsersRead                   = "admin_users::read";
        public const string AdminUsersBan                    = "admin_users::ban";
        public const string AdminUsersTwoFactorReset         = "admin_users::two_factor_reset";

        public const string AdminStartupsRead                = "admin_startups::read";
        public const string AdminStartupsBan                 = "admin_startups::ban";

        public const string AdminSubscriptionsRead           = "admin_subscriptions::read";
        public const string AdminSubscriptionsManage         = "admin_subscriptions::manage";

        public const string AdminPromoCodesRead              = "admin_promo_codes::read";
        public const string AdminPromoCodesManage            = "admin_promo_codes::manage";

        public const string AdminValuationBenchmarksRead     = "admin_valuation_benchmarks::read";
        public const string AdminValuationBenchmarksManage   = "admin_valuation_benchmarks::manage";

        public const string AdminAuditRead                   = "admin_audit::read";

        public const string AdminObservabilityRead           = "admin_observability::read";

        public const string AdminNpdRead                     = "admin_npd::read";
    }
}
