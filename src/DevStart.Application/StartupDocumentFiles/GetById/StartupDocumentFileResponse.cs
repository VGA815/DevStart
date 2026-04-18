using DevStart.Domain.StartupDocumentFiles;

namespace DevStart.Application.StartupDocumentFiles.GetById
{
    public sealed class StartupDocumentFileResponse
    {
        public Guid Id { get; set; }
        public Guid StartupId { get; set; }
        public Guid UploaderId { get; set; }
        public string PresignedUrl { get; set; } = null!;
        public StartupDocumentType DocumentType { get; set; }
        public long FileSize { get; set; }
        public string DocumentName { get; set; } = null!;
        public DateTime UploadDate { get; set; }
    }
}