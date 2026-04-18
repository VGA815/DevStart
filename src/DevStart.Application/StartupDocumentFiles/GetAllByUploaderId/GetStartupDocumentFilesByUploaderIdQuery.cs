using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupDocumentFiles.GetAllByUploaderId
{
    public sealed record GetStartupDocumentFilesByUploaderIdQuery(Guid UploaderId) : IQuery<List<StartupDocumentFileResponse>>;
}
