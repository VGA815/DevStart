using DevStart.SharedKernel;

namespace DevStart.Domain.StartupDocumentFiles
{
    public sealed class StartupDocumentFile : Entity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public Guid StartupId { get; set; }
        public Guid FileId { get; set; }
        public DocumentFileType FileType { get; set; }
        public DateTime UploadDate { get; set; }
        public StartupDocumentFile()
        {
            
        }
        public static StartupDocumentFile Create(string name, Guid startupId, Guid fileId, DocumentFileType documentFileType, DateTime uploadAt)
            => new()
            {
                StartupId = startupId,
                FileId = fileId,
                FileType = documentFileType,
                Id = Guid.NewGuid(),
                Name = name,
                UploadDate = uploadAt
            };
    }
}
