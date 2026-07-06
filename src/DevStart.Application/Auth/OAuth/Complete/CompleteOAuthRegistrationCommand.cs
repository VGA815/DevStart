using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.Register;

namespace DevStart.Application.Auth.OAuth.Complete
{
    // Returns OAuthAuthResult (not a bare TokenPair): a user who enabled 2FA between the consent
    // challenge and its completion must still be challenged for the second factor here.
    public sealed record CompleteOAuthRegistrationCommand(
        string PendingToken,
        List<ConsentItem> Consents,
        string? IpAddress,
        string? UserAgent) : ICommand<OAuthAuthResult>;
}
