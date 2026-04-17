using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Messages.Create
{
    public sealed class CreateMessageCommand : ICommand<Guid>
    {
        public Guid ReceiverId { get; set; }
        public string? TextContent { get; set; }
        public List<Guid>? MediaIds { get; set; }
        public List<Guid>? MetricIds { get; set; }
    }
}
