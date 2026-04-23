using DevStart.Domain.Messages;

namespace DevStart.Application.Messages.GetConversations
{
    public sealed class ConversationSummaryResponse
    {
        public Guid OtherParticipantId { get; set; }
        public ChatParticipantType OtherParticipantType { get; set; }
        public int UnreadCount { get; set; }
        public DateTime LastMessageAt { get; set; }
    }
}
