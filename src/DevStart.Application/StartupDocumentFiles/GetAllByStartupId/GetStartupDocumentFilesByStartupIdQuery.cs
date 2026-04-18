using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupDocumentFiles.GetAllByStartupId
{
    public sealed record GetStartupDocumentFilesByStartupIdQuery(Guid StartupId) : IQuery<List<StartupDocumentFileResponse>>;
}
