using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.TwoFactor;

namespace DevStart.Application.Users.TwoFactor.Setup
{
    public sealed record SetupTwoFactorCommand() : ICommand<TwoFactorSetupData>;
}
