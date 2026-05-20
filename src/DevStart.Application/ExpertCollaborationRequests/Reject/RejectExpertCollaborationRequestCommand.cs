using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ExpertCollaborationRequests.Reject
{
    public sealed class RejectExpertCollaborationRequestCommand : ICommand
    {
        public Guid RequestId { get; set; }

        public RejectExpertCollaborationRequestCommand(Guid requestId)
        {
            RequestId = requestId;
        }
    }
}
