using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ExpertProfiles.GetById
{
    public sealed class GetExpertProfileByIdQuery : IQuery<ExpertProfileResponse>
    {
        public Guid UserId { get; set; }

        /// <summary>
        /// Кто смотрит; null — аноним. Карточка эксперта открыта всем ровно в том же объёме, что и
        /// каталог, — иначе неавторизованный посетитель видит профиль в списке и упирается в
        /// «эксперта не существует» при клике. Непубличный профиль остаётся виден только владельцу:
        /// дашборд читает этим же запросом собственную, ещё не опубликованную карточку.
        /// </summary>
        public Guid? ViewerId { get; set; }

        public GetExpertProfileByIdQuery(Guid userId, Guid? viewerId = null)
        {
            UserId = userId;
            ViewerId = viewerId;
        }
    }
}
