using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.TwoFactor.Enable
{
    public sealed record EnableTwoFactorCommand(string Code) : ICommand<IReadOnlyList<string>>;
}
