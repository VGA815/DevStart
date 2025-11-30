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
    }
}
