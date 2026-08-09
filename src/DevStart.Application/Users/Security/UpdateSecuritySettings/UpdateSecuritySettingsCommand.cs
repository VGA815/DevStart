using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Security;

namespace DevStart.Application.Users.Security.UpdateSecuritySettings
{
    public sealed record UpdateSecuritySettingsCommand(
        TwoFactorStrictness Strictness,
        int TrustDurationDays,
        bool NotifyOnNewDeviceLogin) : ICommand;
}
