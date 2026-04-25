using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentApplications.GetAllByStartupId
{
    public sealed class GetInvestmentApplicationsByStartupIdQuery : IQuery<List<InvestmentApplicationResponse>>
    {
        public Guid StartupId { get; set; }

        public GetInvestmentApplicationsByStartupIdQuery(Guid startupId)
        {
            StartupId = startupId;
        }
    }
}
