using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupMembers.GetAllByStartupId
{
    public sealed record GetStartupMembersByStartupIdQuery(Guid StartupId, Guid ProfileId) : IQuery<List<StartupMemberResponse>>;
}
