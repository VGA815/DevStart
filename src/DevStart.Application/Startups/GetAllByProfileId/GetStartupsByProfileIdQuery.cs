using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Startups.GetAllByProfileId
{
    public sealed record GetStartupsByProfileIdQuery(Guid ProfileId) : IQuery<List<StartupResponse>>;
}
