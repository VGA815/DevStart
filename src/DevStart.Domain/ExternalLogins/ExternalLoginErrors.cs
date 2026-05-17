using DevStart.SharedKernel;

namespace DevStart.Domain.ExternalLogins
{
    public static class ExternalLoginErrors
    {
        public static readonly Error NotFound = Error.NotFound(
            "ExternalLogins.NotFound",
            "The external login was not found");

        public static readonly Error AlreadyLinkedToAnotherUser = Error.Conflict(
            "ExternalLogins.AlreadyLinkedToAnotherUser",
            "This external account is already linked to another user");

        public static readonly Error AlreadyLinked = Error.Conflict(
            "ExternalLogins.AlreadyLinked",
            "This external account is already linked to your user");

        public static readonly Error CannotUnlinkLastCredential = Error.Conflict(
            "ExternalLogins.CannotUnlinkLastCredential",
            "Cannot unlink the only remaining login method. Set a password or link another provider first");

        public static readonly Error EmailMatchesUnverifiedAccount = Error.Conflict(
            "ExternalLogins.EmailMatchesUnverifiedAccount",
            "An account with this email already exists but is not verified. Log in with a password and link the external provider from your account settings");

        public static readonly Error InvalidState = Error.Problem(
            "ExternalLogins.InvalidState",
            "The OAuth state is invalid, expired, or has already been used");

        public static readonly Error ProviderError = Error.Problem(
            "ExternalLogins.ProviderError",
            "The external provider returned an error or invalid response");

        public static readonly Error EmailRequired = Error.Problem(
            "ExternalLogins.EmailRequired",
            "The external provider did not return an email address, which is required for account creation");
    }
}
