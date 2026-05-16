using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ConsentDocuments.GetDocuments;
using DevStart.Domain.UserConsents;

namespace DevStart.Application.ConsentDocuments.GetDocument
{
    /// <summary>
    /// Returns a consent document by type.
    /// If <paramref name="Version"/> is null, returns the currently active document.
    /// </summary>
    public sealed record GetConsentDocumentQuery(ConsentType Type, string? Version = null)
        : IQuery<ConsentDocumentResponse>;
}
