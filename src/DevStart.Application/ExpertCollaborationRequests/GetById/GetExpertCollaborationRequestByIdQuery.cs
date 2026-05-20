using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ExpertCollaborationRequests.GetById
{
    public sealed class GetExpertCollaborationRequestByIdQuery : IQuery<ExpertCollaborationRequestResponse>
    {
        public Guid RequestId { get; set; }

        public GetExpertCollaborationRequestByIdQuery(Guid requestId)
        {
            RequestId = requestId;
        }
    }
}
