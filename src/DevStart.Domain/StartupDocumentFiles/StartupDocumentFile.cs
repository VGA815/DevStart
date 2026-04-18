using DevStart.SharedKernel;

namespace DevStart.Domain.StartupDocumentFiles
{
    public sealed class StartupDocumentFile : Entity
    {
        public Guid Id { get; set; }
        public Guid StartupId { get; set; }
        public Guid UploaderId { get; set; }
        public string ObjectName { get; set; } = null!;
        public string Bucket { get; set; } = null!;
        public StartupDocumentType DocumentType { get; set; }
        public long FileSize { get; set; }
        public string DocumentName { get; set; } = null!;
        public DateTime UploadDate { get; set; }
        public StartupDocumentFile()
        {
            
        }
        public static StartupDocumentFile Create(Guid id, Guid startupId, Guid uploaderId, string objectName, string bucket, StartupDocumentType documentType, long fileSize, string documentName, DateTime uploadDate)
            => new()
            {
                Bucket = bucket,
                DocumentName = documentName,
                DocumentType = documentType,
                FileSize = fileSize,
                Id = id,
                ObjectName = objectName,
                StartupId = startupId,
                UploadDate = uploadDate,
                UploaderId = uploaderId
            };
    }
}
