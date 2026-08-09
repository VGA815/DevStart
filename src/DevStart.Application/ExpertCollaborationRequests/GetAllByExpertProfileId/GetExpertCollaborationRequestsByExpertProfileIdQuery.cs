using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.GetById;
using DevStart.Domain.ExpertCollaborationRequests;

namespace DevStart.Application.ExpertCollaborationRequests.GetAllByExpertProfileId
{
    public sealed class GetExpertCollaborationRequestsByExpertProfileIdQuery : IQuery<List<ExpertCollaborationRequestResponse>>
    {
        public Guid ExpertProfileId { get; set; }
        public ExpertCollaborationRequestStatus? Status { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public GetExpertCollaborationRequestsByExpertProfileIdQuery(
            Guid expertProfileId,
            ExpertCollaborationRequestStatus? status = null,
            int pageNumber = 1,
            int pageSize = 50)
        {
            ExpertProfileId = expertProfileId;
            Status = status;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
