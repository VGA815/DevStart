using DevStart.SharedKernel;

namespace DevStart.Domain.Messages
{
    public sealed class Message : Entity
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public ChatParticipantType SenderType { get; set; }
        public Guid ReceiverId { get; set; }
        public ChatParticipantType ReceiverType { get; set; }
        public string? TextContent { get; set; }
        public List<Guid> MediaIds { get; set; } = [];
        public List<Guid> MetricIds { get; set; } = [];
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Message()
        {

        }
        public void MarkAsRead() => IsRead = true;
        public static Message Create(
            Guid senderId,
            ChatParticipantType senderType,
            Guid receiverId,
            ChatParticipantType receiverType,
            string? textContent,
            List<Guid>? mediaIds,
            List<Guid>? metricIds,
            DateTime createdAt)
            => new()
            {
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                Id = Guid.NewGuid(),
                IsRead = false,
                MediaIds = mediaIds ?? [],
                MetricIds = metricIds ?? [],
                ReceiverId = receiverId,
                ReceiverType = receiverType,
                SenderId = senderId,
                SenderType = senderType,
                TextContent = textContent,
            };
    }
}
