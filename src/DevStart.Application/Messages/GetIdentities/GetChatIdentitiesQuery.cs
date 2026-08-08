using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Messages.GetIdentities
{
    public sealed record GetChatIdentitiesQuery : IQuery<List<ChatIdentityResponse>>;
}
