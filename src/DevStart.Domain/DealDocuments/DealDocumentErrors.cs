using DevStart.SharedKernel;

namespace DevStart.Domain.DealDocuments
{
    public static class DealDocumentErrors
    {
        public static Error NotFound(Guid dealId) => Error.NotFound(
            "DealDocuments.NotFound",
            $"No generated deal document found for deal id = '{dealId}'.");

        public static readonly Error Unauthorized = Error.Problem(
            "DealDocuments.Unauthorized",
            "You are not allowed to access this deal document.");

        public static readonly Error StorageUnavailable = Error.ServiceUnavailable(
            "DealDocuments.StorageUnavailable",
            "The file storage service is temporarily unavailable. Please try again later.");
    }
}
