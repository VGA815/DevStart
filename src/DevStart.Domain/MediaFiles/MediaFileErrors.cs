using DevStart.SharedKernel;

namespace DevStart.Domain.MediaFiles
{
    public static class MediaFileErrors
    {
        public static readonly Error ContentTypeNotAllowed = Error.Validation(
            "MediaFiles.ContentTypeNotAllowed",
            "Only JPEG, PNG, WebP and GIF images can be uploaded.");
        public static readonly Error TooLarge = Error.Validation(
            "MediaFiles.FileTooLarge",
            $"File size exceeds the maximum allowed size of {MediaFileRules.MaxFileSizeBytes / (1024 * 1024)} MB.");
        public static readonly Error Empty = Error.Validation(
            "MediaFiles.Empty",
            "The file is empty.");
        public static Error NotFound(Guid fileId) => Error.NotFound(
            "MediaFiles.NotFound",
            $"The media file with Id = '{fileId}' was not found");
        public static readonly Error NotFoundByUploaderId = Error.NotFound(
            "MediaFiles.NotFoundByUploaderId",
            "The media file with specified uploaderId was not found");
        public static readonly Error StorageUnavailable = Error.ServiceUnavailable(
            "MediaFiles.StorageUnavailable",
            "The file storage service is temporarily unavailable. Please try again later.");
    }
}
