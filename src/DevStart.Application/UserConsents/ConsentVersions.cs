using DevStart.Domain.UserConsents;

namespace DevStart.Application.UserConsents
{
    public static class ConsentVersions
    {
        public const string PersonalDataProcessing = "1.0";
        public const string PrivacyPolicy          = "1.0";
        public const string TermsOfService         = "1.0";
        public const string Cookies                = "1.0";
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
