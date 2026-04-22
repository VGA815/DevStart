using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups.GetAllByProfileId;

namespace DevStart.Application.StartupFollowers.GetAllByProfileId
{
    public sealed record GetStartupsByProfileFollowsQuery(Guid ProfileId) : IQuery<List<StartupResponse>>;
}
