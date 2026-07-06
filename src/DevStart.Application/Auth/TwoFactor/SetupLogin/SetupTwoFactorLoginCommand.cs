using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Auth.TwoFactor.SetupLogin
{
    public sealed record SetupTwoFactorLoginCommand(string PendingToken) : ICommand<TwoFactorLoginSetupResponse>;

    /// <summary>
    /// The pending token is echoed back so the client carries a single value into the confirm step.
    /// </summary>
    public sealed record TwoFactorLoginSetupResponse(string Secret, string OtpAuthUri, string PendingToken);
}
