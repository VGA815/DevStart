using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.TwoFactor.Disable
{
    /// <summary>
    /// Password is required for accounts that have one (OAuth-only accounts pass null); the code
    /// may be a 6-digit TOTP or a recovery code.
    /// </summary>
    public sealed record DisableTwoFactorCommand(string? Password, string Code) : ICommand;
}
