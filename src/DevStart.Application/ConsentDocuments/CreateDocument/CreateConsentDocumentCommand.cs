using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.UserConsents;

namespace DevStart.Application.ConsentDocuments.CreateDocument
{
    public sealed record CreateConsentDocumentCommand(
        ConsentType Type,
        string Version,
        string Title,
        string Content) : ICommand<Guid>;
}
