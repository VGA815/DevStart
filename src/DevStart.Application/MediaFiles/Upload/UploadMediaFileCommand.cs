using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.MediaFiles.Upload
{
    public sealed class UploadMediaFileCommand : ICommand<Guid>
    {
        public Guid OwnerId { get; set; }
        public Stream FileStream { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long Size { get; set; }

        public UploadMediaFileCommand(Guid ownerId, Stream fileStream, string contentType, long size)
        {
            OwnerId = ownerId;
            FileStream = fileStream;
            ContentType = contentType;
            Size = size;
        }
    }
}
