using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupInvestors.GetById
{
    public sealed record GetStartupInvestorByIdQuery(Guid StartupId, Guid ProfileId) : IQuery<StartupInvestorResponse>;
}
