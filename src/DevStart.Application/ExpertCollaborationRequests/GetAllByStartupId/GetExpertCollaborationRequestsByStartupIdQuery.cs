using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.GetById;
using DevStart.Domain.ExpertCollaborationRequests;

namespace DevStart.Application.ExpertCollaborationRequests.GetAllByStartupId
{
    public sealed class GetExpertCollaborationRequestsByStartupIdQuery : IQuery<List<ExpertCollaborationRequestResponse>>
    {
        public Guid StartupId { get; set; }
        public ExpertCollaborationRequestStatus? Status { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public GetExpertCollaborationRequestsByStartupIdQuery(
            Guid startupId,
            ExpertCollaborationRequestStatus? status = null,
            int pageNumber = 1,
            int pageSize = 50)
        {
            StartupId = startupId;
            Status = status;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
