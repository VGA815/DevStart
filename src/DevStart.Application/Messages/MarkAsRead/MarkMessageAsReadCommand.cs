using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Messages.MarkAsRead
{
    public sealed record MarkMessageAsReadCommand(Guid MessageId) : ICommand;
}
