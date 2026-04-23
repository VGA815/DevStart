using DevStart.SharedKernel;

namespace DevStart.Domain.Messages
{
    public static class MessageErrors
    {
        public static Error NotFound(Guid messageId) => Error.NotFound(
            "Messages.NotFound",
            $"The message with the id = '{messageId}' was not found.");
        public static readonly Error IsEmpty = Error.Problem(
            "Messages.IsEmpty",
            "The message does not have any content.");
        public static readonly Error Unauthorized = Error.Problem(
            "Messages.Unauthorized",
            "You are not allowed to perform this action on this message.");
        public static Error ReceiverNotFound(Guid receiverId, ChatParticipantType receiverType) => Error.NotFound(
            "Messages.ReceiverNotFound",
            $"Receiver of type '{receiverType}' with id = '{receiverId}' was not found.");
    }
}
