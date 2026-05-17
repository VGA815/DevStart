using DevStart.SharedKernel;

namespace DevStart.Domain.UserConsents
{
    public static class UserConsentErrors
    {
        public static readonly Error MandatoryConsentNotAccepted = Error.Validation(
            "UserConsents.MandatoryConsentNotAccepted",
            "All mandatory consents (Personal Data Processing, Privacy Policy, Terms of Service) must be accepted");

        public static readonly Error CannotRevokeMandatoryConsent = Error.Validation(
            "UserConsents.CannotRevokeMandatoryConsent",
            "Mandatory consents cannot be revoked. To remove your data, please delete your account");

        public static Error ConsentVersionMismatch(ConsentType type, string expectedVersion) => Error.Validation(
            "UserConsents.ConsentVersionMismatch",
            $"The document version for consent '{type}' is outdated. Expected version: {expectedVersion}");

        public static Error ConsentNotFound(ConsentType type) => Error.NotFound(
            "UserConsents.ConsentNotFound",
            $"No active consent found for type '{type}'");
    }
}
