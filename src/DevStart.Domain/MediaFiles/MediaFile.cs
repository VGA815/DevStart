using DevStart.SharedKernel;

namespace DevStart.Domain.MediaFiles
{
    public sealed class MediaFile : Entity
    {
        public Guid Id { get; set; }
        public Guid UploaderId { get; set; }
        public string FileUrl { get; set; }
        public string Name { get; set; }
        public MediaFileType FileType { get; set; }
        public int FileSize { get; set; }
        public DateTime UploadDate { get; set; }
    }
}
