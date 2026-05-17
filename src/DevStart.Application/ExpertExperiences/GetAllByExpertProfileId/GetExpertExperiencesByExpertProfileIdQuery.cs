using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ExpertExperiences.GetAllByExpertProfileId
{
    public sealed class GetExpertExperiencesByExpertProfileIdQuery : IQuery<List<ExpertExperienceResponse>>
    {
        public Guid ExpertProfileId { get; set; }

        public GetExpertExperiencesByExpertProfileIdQuery(Guid expertProfileId)
        {
            ExpertProfileId = expertProfileId;
        }
    }
}
