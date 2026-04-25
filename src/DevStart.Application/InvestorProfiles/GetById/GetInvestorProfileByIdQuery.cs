using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestorProfiles.GetById
{
    public sealed class GetInvestorProfileByIdQuery : IQuery<InvestorProfileResponse>
    {
        public Guid UserId { get; set; }

        public GetInvestorProfileByIdQuery(Guid userId)
        {
            UserId = userId;
        }
    }
}
