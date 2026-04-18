using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupDocumentFiles.Delete
{
    public sealed record DeleteStartupDocumentFileCommand(Guid StartupDocumentFileId) : ICommand;
}