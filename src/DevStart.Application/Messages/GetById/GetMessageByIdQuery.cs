using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Messages.GetById
{
    public sealed record GetMessageByIdQuery(Guid MessageId) : IQuery<MessageResponse>;
}
