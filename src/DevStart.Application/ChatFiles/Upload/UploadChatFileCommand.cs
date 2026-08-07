using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ChatFiles.Upload
{
    public sealed class UploadChatFileCommand : ICommand<ChatFileResponse>
    {
        public Stream FileStream { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }

        public UploadChatFileCommand(Stream fileStream, string fileName, string contentType, long fileSize)
        {
            FileStream = fileStream;
            FileName = fileName;
            ContentType = contentType;
            FileSize = fileSize;
        }
    }
}
