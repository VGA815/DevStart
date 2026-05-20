using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.GetById;

namespace DevStart.Application.ExpertCollaborationRequests.GetAllByStartupId
{
    public sealed class GetExpertCollaborationRequestsByStartupIdQuery : IQuery<List<ExpertCollaborationRequestResponse>>
    {
        public Guid StartupId { get; set; }

        public GetExpertCollaborationRequestsByStartupIdQuery(Guid startupId)
        {
            StartupId = startupId;
        }
    }
}
