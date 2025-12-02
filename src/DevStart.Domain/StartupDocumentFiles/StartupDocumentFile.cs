using DevStart.SharedKernel;

namespace DevStart.Domain.StartupDocumentFiles
{
    public sealed class StartupDocumentFile : Entity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid StartupId { get; set; }
        public string FileUrl { get; set; }
        public DocumentFileType FileType { get; set; }
        public DateTime UploadDate { get; set; }
    }
}
