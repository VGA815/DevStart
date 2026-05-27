using DevStart.SharedKernel;

namespace DevStart.Domain.MediaFiles
{
    public static class MediaFileErrors
    {
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
