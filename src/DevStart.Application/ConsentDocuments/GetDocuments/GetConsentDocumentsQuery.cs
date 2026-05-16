using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ConsentDocuments.GetDocuments
{
    /// <summary>Returns all currently active consent documents.</summary>
    public sealed record GetConsentDocumentsQuery : IQuery<List<ConsentDocumentResponse>>;
}
