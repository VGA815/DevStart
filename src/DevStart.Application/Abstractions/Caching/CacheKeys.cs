namespace DevStart.Application.Abstractions.Caching
{
    public static class CacheKeys
    {
        private const string Version = "v2";

        public static string User(Guid userId) => $"{Version}:users:{userId}";

        public static string Profile(Guid userId) => $"{Version}:profiles:{userId}";

        public static string Startup(Guid startupId) => $"{Version}:startups:{startupId}";

        public static string StartupProduct(Guid startupProductId) => $"{Version}:startup-products:{startupProductId}";

        public static string MediaFile(Guid fileId) => $"{Version}:media-files:{fileId}";

        public static string Notification(Guid notificationId) => $"{Version}:notifications:{notificationId}";

        public static string UserPreference(Guid userPreferenceId) => $"{Version}:user-preferences:{userPreferenceId}";
    }
}
