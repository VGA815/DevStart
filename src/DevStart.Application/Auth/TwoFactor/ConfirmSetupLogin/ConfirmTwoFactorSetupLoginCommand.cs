using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.OAuth;

namespace DevStart.Application.Auth.TwoFactor.ConfirmSetupLogin
{
    public sealed record ConfirmTwoFactorSetupLoginCommand(
        string PendingToken,
        string Code,
        string? IpAddress,
        string? UserAgent) : ICommand<TwoFactorSetupCompleteResponse>;

    /// <summary>
    /// Recovery codes are returned exactly once, alongside the auth outcome (tokens, or a consent
    /// challenge when mandatory consents are outdated).
    /// </summary>
    public sealed record TwoFactorSetupCompleteResponse(IReadOnlyList<string> RecoveryCodes, OAuthAuthResult Auth);
}
