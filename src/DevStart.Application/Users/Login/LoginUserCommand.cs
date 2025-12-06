using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.Login
{
    public sealed record LoginUserCommand(string Email, string Password) : ICommand<string>;
}
