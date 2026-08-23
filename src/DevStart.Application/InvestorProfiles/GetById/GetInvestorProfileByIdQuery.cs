using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestorProfiles.GetById
{
    public sealed class GetInvestorProfileByIdQuery : IQuery<InvestorProfileResponse>
    {
        public Guid UserId { get; set; }

        /// <summary>
        /// Кто смотрит; null — аноним. Карточка инвестора открыта всем ровно в том же объёме, что и
        /// каталог, — иначе неавторизованный посетитель видит профиль в списке и упирается в
        /// «инвестора не существует» при клике. Непубличный профиль остаётся виден только владельцу:
        /// дашборд читает этим же запросом собственную, ещё не опубликованную карточку.
        /// </summary>
        public Guid? ViewerId { get; set; }

        public GetInvestorProfileByIdQuery(Guid userId, Guid? viewerId = null)
        {
            UserId = userId;
            ViewerId = viewerId;
        }
    }
}
