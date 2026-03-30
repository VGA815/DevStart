using DevStart.SharedKernel;

namespace DevStart.Domain.MediaFiles
{
    public sealed class MediaFile : Entity
    {
        public Guid Id { get; set; }
        public Guid UploaderId { get; set; }
        public string ObjectName { get; set; } = null!;
        public string Bucket { get; set; } = null!;
        public MediaFileType FileType { get; set; }
        public int FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public MediaFile()
        {
            
        }
        public static MediaFile Create(Guid uploaderId, string objectName, string bucket, MediaFileType fileType, int fiSize, DateTime uploadDate)
            => new()
            {
                Bucket = bucket,
                FileSize = fiSize,
                FileType = fileType,
                Id = Guid.NewGuid(),
                ObjectName = objectName,
                UploadDate = uploadDate,
                UploaderId = uploaderId
            };
    }
}
