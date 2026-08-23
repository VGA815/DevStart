using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ExpertExperiences.GetAllByExpertProfileId
{
    public sealed class GetExpertExperiencesByExpertProfileIdQuery : IQuery<List<ExpertExperienceResponse>>
    {
        public Guid ExpertProfileId { get; set; }

        /// <summary>
        /// Кто смотрит; null — аноним. Опыт — основное содержимое публичной карточки эксперта, поэтому
        /// он открыт там же, где и она: у непубличного профиля — только владельцу, который правит этот
        /// же список в дашборде.
        /// </summary>
        public Guid? ViewerId { get; set; }

        public GetExpertExperiencesByExpertProfileIdQuery(Guid expertProfileId, Guid? viewerId = null)
        {
            ExpertProfileId = expertProfileId;
            ViewerId = viewerId;
        }
    }
}
