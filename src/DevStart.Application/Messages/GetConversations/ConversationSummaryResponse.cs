namespace DevStart.Application.Messages.GetConversations
{
    public sealed class ConversationSummaryResponse
    {
        public Guid OtherUserId { get; set; }
        public int UnreadCount { get; set; }
        public DateTime LastMessageAt { get; set; }
    }
}
