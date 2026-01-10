using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupInvestors.GetAllByStartupId
{
    public sealed record GetStartupInvestorsByStartupIdQuery(Guid StartupId) : IQuery<List<StartupInvestorResponse>>;
}
