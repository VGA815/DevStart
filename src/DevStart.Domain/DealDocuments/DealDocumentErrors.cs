using DevStart.SharedKernel;

namespace DevStart.Domain.DealDocuments
{
    public static class DealDocumentErrors
    {
        public static Error NotFound(Guid dealId) => Error.NotFound(
            "DealDocuments.NotFound",
            $"No generated deal document found for deal id = '{dealId}'.");

        /// <summary>
        /// The document set predates PDF rendering and has not been filled in yet. Distinct from
        /// <see cref="NotFound"/>: the term sheet exists, this rendering of it does not.
        /// </summary>
        public static Error PdfNotGenerated(Guid dealId) => Error.NotFound(
            "DealDocuments.PdfNotGenerated",
            $"The PDF term sheet for deal id = '{dealId}' has not been generated yet.");

        public static readonly Error Unauthorized = Error.Problem(
            "DealDocuments.Unauthorized",
            "You are not allowed to access this deal document.");

        public static readonly Error StorageUnavailable = Error.ServiceUnavailable(
            "DealDocuments.StorageUnavailable",
            "The file storage service is temporarily unavailable. Please try again later.");
    }
}
