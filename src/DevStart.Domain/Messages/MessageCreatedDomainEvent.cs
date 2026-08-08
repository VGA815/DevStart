using DevStart.SharedKernel;

namespace DevStart.Domain.Messages
{
    public sealed record MessageCreatedDomainEvent(
        Guid MessageId,
        Guid SenderId,
        ChatParticipantType SenderType,
        Guid? SentByProfileId,
        Guid ReceiverId,
        ChatParticipantType ReceiverType) : IDomainEvent;
}
