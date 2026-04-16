using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InviteTokens.GetAllByStartupId
{
    public sealed record GetInviteTokensByStartupIdQuery(Guid StartupId) : IQuery<List<InviteTokenResponse>>;
}
