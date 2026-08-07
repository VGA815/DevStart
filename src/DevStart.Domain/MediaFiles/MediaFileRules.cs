namespace DevStart.Domain.MediaFiles
{
    /// <summary>Limits for avatar/image uploads.</summary>
    public static class MediaFileRules
    {
        public const long MaxFileSizeBytes = 10 * 1024 * 1024;

        /// <summary>Content type to stored extension. Also acts as the allow-list.</summary>
        private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["image/gif"] = ".gif",
        };

        public static bool IsAllowedContentType(string? contentType) =>
            !string.IsNullOrWhiteSpace(contentType) && ExtensionByContentType.ContainsKey(contentType.Trim());

        public static string ExtensionFor(string contentType) =>
            ExtensionByContentType.TryGetValue(contentType.Trim(), out string? extension) ? extension : ".bin";

        public static MediaFileType TypeFor(string contentType) =>
            contentType.Trim().Equals("image/gif", StringComparison.OrdinalIgnoreCase)
                ? MediaFileType.Gif
                : MediaFileType.Img;
    }
}
