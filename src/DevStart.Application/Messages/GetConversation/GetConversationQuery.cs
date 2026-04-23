using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Messages.GetById;
using DevStart.Domain.Messages;

namespace DevStart.Application.Messages.GetConversation
{
    public sealed record GetConversationQuery(
        ChatParticipantType OtherType,
        Guid OtherId,
        Guid? AsStartupId,
        int Page,
        int PageSize) : IQuery<List<MessageResponse>>;
}
