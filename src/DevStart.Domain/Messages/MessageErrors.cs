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
    }
}
