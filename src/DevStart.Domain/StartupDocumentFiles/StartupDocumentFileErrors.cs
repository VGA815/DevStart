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
    }
}
