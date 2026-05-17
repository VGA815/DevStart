using DevStart.Domain.ExternalLogins;

namespace DevStart.Application.Abstractions.Authentication
{
    public sealed record OAuthStateEntry(
        ExternalLoginProvider Provider,
        string CodeVerifier,
        string RedirectUri,
        Guid? LinkUserId);
}
