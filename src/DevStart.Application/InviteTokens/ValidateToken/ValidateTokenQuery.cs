using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InviteTokens.ValidateToken
{
    public sealed record ValidateTokenQuery(Guid TokenId) : IQuery<bool>;
}
