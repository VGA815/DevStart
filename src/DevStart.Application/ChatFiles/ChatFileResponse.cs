namespace DevStart.Application.ChatFiles
{
    public sealed class ChatFileResponse
    {
        public Guid Id { get; set; }
        public Guid UploaderId { get; set; }
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }
        public string PresignedUrl { get; set; } = null!;
        public DateTime UploadDate { get; set; }
    }
}
