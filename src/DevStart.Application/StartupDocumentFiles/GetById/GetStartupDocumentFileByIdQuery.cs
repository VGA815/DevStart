using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupDocumentFiles.GetById
{
    public sealed record GetStartupDocumentFileByIdQuery(Guid StartupDocumentFileId, int Expires) : IQuery<StartupDocumentFileResponse>;
}
