using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.MediaFiles.Delete
{
    public sealed record DeleteMediaFileCommand(Guid FileId) : ICommand;
}
