namespace DevStart.Application.MediaFiles.GetById
{
    public sealed class MediaFileResponse
    {
        public Guid Id { get; set; }
        public Guid UploaderId { get; set; }
        public string PresignedUrl { get; set; } = null!;
        public int FileSize { get; set; }
        public DateTime UploadDate { get; set; }
    }
}