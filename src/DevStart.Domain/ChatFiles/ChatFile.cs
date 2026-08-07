using DevStart.SharedKernel;

namespace DevStart.Domain.ChatFiles
{
    /// <summary>
    /// An arbitrary file uploaded straight from a user's machine to be attached to a chat message.
    /// Unlike <c>MediaFile</c> (avatars, images only) it keeps the original name and content type, and
    /// unlike <c>StartupDocumentFile</c> it is not owned by a startup.
    /// </summary>
    public sealed class ChatFile : Entity
    {
        public Guid Id { get; set; }
        public Guid UploaderId { get; set; }

        /// <summary>
        /// The message this file was sent with, or <c>null</c> while it is still an unsent draft attachment.
        /// Read access is derived from it: a draft is visible to its uploader only, a sent file to everyone
        /// who may read the message.
        /// </summary>
        public Guid? MessageId { get; set; }

        public string ObjectName { get; set; } = null!;
        public string Bucket { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }

        public ChatFile()
        {

        }

        public void AttachTo(Guid messageId) => MessageId = messageId;

        public static ChatFile Create(
            Guid id,
            Guid uploaderId,
            string objectName,
            string bucket,
            string fileName,
            string contentType,
            long fileSize,
            DateTime uploadDate)
            => new()
            {
                Id = id,
                UploaderId = uploaderId,
                MessageId = null,
                ObjectName = objectName,
                Bucket = bucket,
                FileName = fileName,
                ContentType = contentType,
                FileSize = fileSize,
                UploadDate = uploadDate,
            };
    }
}
