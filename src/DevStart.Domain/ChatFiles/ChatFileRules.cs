namespace DevStart.Domain.ChatFiles
{
    /// <summary>Limits applied to files uploaded from a user's machine into a chat.</summary>
    public static class ChatFileRules
    {
        public const string Bucket = "chat-files";

        public const long MaxFileSizeBytes = 25 * 1024 * 1024;

        public const int MaxFileNameLength = 260;

        /// <summary>Formats a user could plausibly send in a founder/investor conversation.</summary>
        public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/gif",
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/vnd.oasis.opendocument.text",
            "application/vnd.oasis.opendocument.spreadsheet",
            "application/vnd.oasis.opendocument.presentation",
            "application/rtf",
            "application/zip",
            "application/x-zip-compressed",
            "text/plain",
            "text/csv",
            "text/markdown",
        };

        public static bool IsAllowedContentType(string? contentType) =>
            !string.IsNullOrWhiteSpace(contentType) && AllowedContentTypes.Contains(contentType.Trim());

        public static bool IsImage(string? contentType) =>
            !string.IsNullOrWhiteSpace(contentType) &&
            contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
