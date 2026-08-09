using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Auth.Sessions.RevokeSession
{
    public sealed record RevokeSessionCommand(Guid SessionId) : ICommand;
}
