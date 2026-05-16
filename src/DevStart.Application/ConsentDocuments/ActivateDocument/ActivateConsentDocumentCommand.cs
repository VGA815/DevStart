using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ConsentDocuments.ActivateDocument
{
    public sealed record ActivateConsentDocumentCommand(Guid DocumentId) : ICommand;
}
