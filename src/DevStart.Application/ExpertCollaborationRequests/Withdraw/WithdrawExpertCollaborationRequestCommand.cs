using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ExpertCollaborationRequests.Withdraw
{
    public sealed class WithdrawExpertCollaborationRequestCommand : ICommand
    {
        public Guid RequestId { get; set; }

        public WithdrawExpertCollaborationRequestCommand(Guid requestId)
        {
            RequestId = requestId;
        }
    }
}
