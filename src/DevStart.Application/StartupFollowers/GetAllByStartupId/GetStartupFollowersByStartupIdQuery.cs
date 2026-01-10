using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupFollowers.GetAllByStartupId
{
    public sealed record GetStartupFollowersByStartupIdQuery(Guid StartupId, int Page, int PageSize) : IQuery<List<StartupFollowerResponse>>;
}
