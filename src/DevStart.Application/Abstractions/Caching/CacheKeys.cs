namespace DevStart.Application.Abstractions.Caching
{
    public static class CacheKeys
    {
        private const string Version = "v2";

        public static string User(Guid userId) => $"{Version}:users:{userId}";

        public static string Profile(Guid userId) => $"{Version}:profiles:{userId}";

        public static string Startup(Guid startupId) => $"{Version}:startups:{startupId}";

        public static string StartupProduct(Guid startupProductId) => $"{Version}:startup-products:{startupProductId}";

        public static string StartupDocumentFile(Guid startupDocumentFileId) => $"{Version}:startup-document-file:{startupDocumentFileId}";

        public static string MediaFile(Guid fileId) => $"{Version}:media-files:{fileId}";

        public static string UserPreference(Guid userPreferenceId) => $"{Version}:user-preferences:{userPreferenceId}";

        public static string StartupMetric(Guid metricId) => $"{Version}:startup-metrics:{metricId}";

        public static string StartupRoadmapItem(Guid itemId) => $"{Version}:startup-roadmap-items:{itemId}";
    }
}
