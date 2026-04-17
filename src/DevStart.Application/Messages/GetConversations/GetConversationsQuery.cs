using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Messages.GetConversations
{
    public sealed record GetConversationsQuery(int Page, int PageSize) : IQuery<List<ConversationSummaryResponse>>;
}
