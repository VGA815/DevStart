using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Messages.GetConversation
{
    public sealed record GetConversationQuery(Guid OtherUserId, int Page, int PageSize) : IQuery<List<MessageResponse>>;
}
