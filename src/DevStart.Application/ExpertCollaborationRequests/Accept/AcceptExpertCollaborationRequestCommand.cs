using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ExpertCollaborationRequests.Accept
{
    public sealed class AcceptExpertCollaborationRequestCommand : ICommand
    {
        public Guid RequestId { get; set; }

        public AcceptExpertCollaborationRequestCommand(Guid requestId)
        {
            RequestId = requestId;
        }
    }
}
