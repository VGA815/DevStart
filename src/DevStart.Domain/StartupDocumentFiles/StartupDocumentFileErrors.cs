using DevStart.SharedKernel;

namespace DevStart.Domain.StartupDocumentFiles
{
    public static class StartupDocumentFileErrors
    {
        public static Error NotFound(Guid fileId) => Error.NotFound(
            "StartupDocumentFiles.NotFound",
            $"The startup document file with Id = '{fileId}' was not found");
        public static readonly Error NotFoundByStartupId = Error.NotFound(
            "StartupDocumentFiles.NotFoundByStartupId",
            "The startup document file with the specified startupId was not found");
        public static readonly Error Forbidden = Error.Forbidden(
            "StartupDocumentFiles.Forbidden",
            "You are not allowed to access these documents.");
        public static readonly Error StorageUnavailable = Error.ServiceUnavailable(
            "StartupDocumentFiles.StorageUnavailable",
            "The file storage service is temporarily unavailable. Please try again later.");
    }
}
