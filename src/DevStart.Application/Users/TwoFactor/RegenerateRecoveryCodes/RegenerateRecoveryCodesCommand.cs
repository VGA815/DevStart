using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.TwoFactor.RegenerateRecoveryCodes
{
    /// <summary>Requires a current TOTP code (recovery codes cannot mint their own replacements).</summary>
    public sealed record RegenerateRecoveryCodesCommand(string Code) : ICommand<IReadOnlyList<string>>;
}
