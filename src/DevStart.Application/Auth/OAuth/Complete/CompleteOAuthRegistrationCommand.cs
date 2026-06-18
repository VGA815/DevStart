using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Users.Register;

namespace DevStart.Application.Auth.OAuth.Complete
{
    public sealed record CompleteOAuthRegistrationCommand(
        string PendingToken,
        List<ConsentItem> Consents,
        string? IpAddress,
        string? UserAgent) : ICommand<TokenPair>;
}
