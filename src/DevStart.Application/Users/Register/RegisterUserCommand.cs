using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.Register
{
    public sealed record RegisterUserCommand(string Email, string Password, string Username)
        : ICommand<Guid>;
}
