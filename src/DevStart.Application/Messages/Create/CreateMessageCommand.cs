using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Messages;

namespace DevStart.Application.Messages.Create
{
    public sealed class CreateMessageCommand : ICommand<Guid>
    {
        public Guid ReceiverId { get; set; }
        public ChatParticipantType ReceiverType { get; set; }
        public Guid? SenderStartupId { get; set; }
        public string? TextContent { get; set; }
        public List<Guid>? MediaIds { get; set; }
        public List<Guid>? MetricIds { get; set; }
        public List<Guid>? DocumentIds { get; set; }
        public List<Guid>? FileIds { get; set; }
    }
}
