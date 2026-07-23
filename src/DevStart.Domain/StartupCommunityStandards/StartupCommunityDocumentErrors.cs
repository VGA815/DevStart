using DevStart.SharedKernel;

namespace DevStart.Domain.StartupCommunityStandards
{
    public static class StartupCommunityDocumentErrors
    {
        public static readonly Error Unauthorized = Error.Forbidden(
            "StartupCommunityDocuments.Unauthorized",
            "Only founders and administrators of the startup can manage its community documents");

        public static Error NotFound(Guid startupId, CommunityDocumentType type) => Error.NotFound(
            "StartupCommunityDocuments.NotFound",
            $"Startup '{startupId}' has no community document of type '{type}'");

        public static readonly Error TemplateNotFound = Error.NotFound(
            "StartupCommunityDocuments.TemplateNotFound",
            "No starter template is available for the requested community document type");
    }
}
