using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Auth.Logout
{
    public sealed record LogoutCommand(string RefreshToken) : ICommand;
}
