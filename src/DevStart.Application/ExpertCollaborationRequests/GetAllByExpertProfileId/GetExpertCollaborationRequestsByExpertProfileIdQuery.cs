using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.GetById;

namespace DevStart.Application.ExpertCollaborationRequests.GetAllByExpertProfileId
{
    public sealed class GetExpertCollaborationRequestsByExpertProfileIdQuery : IQuery<List<ExpertCollaborationRequestResponse>>
    {
        public Guid ExpertProfileId { get; set; }

        public GetExpertCollaborationRequestsByExpertProfileIdQuery(Guid expertProfileId)
        {
            ExpertProfileId = expertProfileId;
        }
    }
}
