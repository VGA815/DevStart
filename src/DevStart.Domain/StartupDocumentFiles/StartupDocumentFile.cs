using DevStart.SharedKernel;

namespace DevStart.Domain.StartupDocumentFiles
{
    public sealed class StartupDocumentFile : Entity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid StartupId { get; set; }
        public string FileUrl { get; set; } = null!;
        public DocumentFileType FileType { get; set; }
        public DateTime UploadDate { get; set; }
    }
}
