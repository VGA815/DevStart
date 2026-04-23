using DevStart.Domain.Messages;

namespace DevStart.Application.Messages.GetById
{
    public sealed class MessageResponse
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
    }
}
