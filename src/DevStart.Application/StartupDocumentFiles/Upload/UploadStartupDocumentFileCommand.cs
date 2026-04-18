using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupDocumentFiles;

namespace DevStart.Application.StartupDocumentFiles.Upload
{
    public sealed class UploadStartupDocumentFileCommand : ICommand<Guid>
    {
        public Guid StartupId { get; set; }
        public StartupDocumentType DocumentType { get; set; }
        public long FileSize { get; set; }
        public Stream FileStream { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public string Bucket { get; set; } = null!;
        public string DocumentName { get; set; } = null!;

        public UploadStartupDocumentFileCommand(
            Guid startupId, StartupDocumentType documentType, long fileSize, Stream fileStream, string contentType, string bucket, string documentName)
        {
            StartupId = startupId;
            DocumentType = documentType;
            FileSize = fileSize;
            FileStream = fileStream;
            ContentType = contentType;
            Bucket = bucket;
            DocumentName = documentName;
        }
    }
}
