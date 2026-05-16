using DevStart.Domain.UserConsents;
using DevStart.SharedKernel;

namespace DevStart.Domain.ConsentDocuments
{
    public static class ConsentDocumentErrors
    {
        public static Error NotFound(Guid id) => Error.NotFound(
            "ConsentDocuments.NotFound",
            $"Consent document with id '{id}' was not found");

        public static Error VersionAlreadyExists(ConsentType type, string version) => Error.Conflict(
            "ConsentDocuments.VersionAlreadyExists",
            $"A consent document of type '{type}' with version '{version}' already exists");

        public static Error NoActiveDocument(ConsentType type) => Error.NotFound(
            "ConsentDocuments.NoActiveDocument",
            $"No active consent document found for type '{type}'");
    }
}
