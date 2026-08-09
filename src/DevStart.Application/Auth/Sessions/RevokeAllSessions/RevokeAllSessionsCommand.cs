using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Auth.Sessions.RevokeAllSessions
{
    /// <summary>
    /// <paramref name="IncludeCurrent"/> defaults to false: signing the user out of the very tab they
    /// pressed the button in reads as a bug, not as security.
    /// </summary>
    public sealed record RevokeAllSessionsCommand(bool IncludeCurrent = false) : ICommand;
}
