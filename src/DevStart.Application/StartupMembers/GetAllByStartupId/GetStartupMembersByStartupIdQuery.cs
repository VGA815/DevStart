using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupMembers.GetAllByStartupId
{
    public sealed record GetStartupMembersByStartupIdQuery(Guid StartupId) : IQuery<List<StartupMemberResponse>>;
}
