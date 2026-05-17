using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ExpertProfiles.GetById
{
    public sealed class GetExpertProfileByIdQuery : IQuery<ExpertProfileResponse>
    {
        public Guid UserId { get; set; }

        public GetExpertProfileByIdQuery(Guid userId)
        {
            UserId = userId;
        }
    }
}
