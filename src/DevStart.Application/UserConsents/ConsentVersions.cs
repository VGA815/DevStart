using DevStart.Domain.UserConsents;

namespace DevStart.Application.UserConsents
{
    public static class ConsentVersions
    {
        public const string PersonalDataProcessing = "1.0";
        public const string PrivacyPolicy          = "1.0";
        public const string TermsOfService         = "1.0";
        // 1.1 replaced the never-implemented `_analytics_id` cookie with the real Matomo cookies.
        // Cookies is deliberately absent from MandatoryTypes below, so bumping it re-prompts
        // nobody at login; on an existing database the new document seeds INACTIVE and must be
        // activated via PATCH api/consent-documents/{id}/activate (see DEPLOYMENT.md).
        public const string Cookies                = "1.1";
        public const string PublicOffer            = "1.0";

        public static string GetCurrentVersion(ConsentType type) => type switch
        {
            ConsentType.PersonalDataProcessing => PersonalDataProcessing,
            ConsentType.PrivacyPolicy          => PrivacyPolicy,
            ConsentType.TermsOfService         => TermsOfService,
            ConsentType.Cookies                => Cookies,
            ConsentType.PublicOffer            => PublicOffer,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static readonly IReadOnlySet<ConsentType> MandatoryTypes = new HashSet<ConsentType>
        {
            ConsentType.PersonalDataProcessing,
            ConsentType.PrivacyPolicy,
            ConsentType.TermsOfService,
            ConsentType.PublicOffer
        };
    }
}
