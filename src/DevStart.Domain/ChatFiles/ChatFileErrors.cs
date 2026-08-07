using DevStart.SharedKernel;

namespace DevStart.Domain.ChatFiles
{
    public static class ChatFileErrors
    {
        public static Error NotFound(Guid fileId) => Error.NotFound(
            "ChatFiles.NotFound",
            $"The chat file with Id = '{fileId}' was not found.");

        public static readonly Error Unauthorized = Error.Forbidden(
            "ChatFiles.Unauthorized",
            "You are not allowed to access this chat file.");

        public static readonly Error ContentTypeNotAllowed = Error.Validation(
            "ChatFiles.ContentTypeNotAllowed",
            "This file type cannot be sent in a chat.");

        public static readonly Error TooLarge = Error.Validation(
            "ChatFiles.TooLarge",
            $"File size exceeds the maximum allowed size of {ChatFileRules.MaxFileSizeBytes / (1024 * 1024)} MB.");

        public static readonly Error Empty = Error.Validation(
            "ChatFiles.Empty",
            "The file is empty.");

        public static readonly Error AlreadyAttached = Error.Problem(
            "ChatFiles.AlreadyAttached",
            "This file has already been sent in another message.");

        public static readonly Error StorageUnavailable = Error.ServiceUnavailable(
            "ChatFiles.StorageUnavailable",
            "The file storage service is temporarily unavailable. Please try again later.");
    }
}
