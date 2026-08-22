using DevStart.SharedKernel;

namespace DevStart.Domain.StartupPartnerships
{
    public static class StartupPartnershipErrors
    {
        public static Error NotFound(Guid partnershipId) => Error.NotFound(
            "StartupPartnerships.NotFound",
            $"The startup partnership with id = '{partnershipId}' was not found.");

        public static readonly Error Unauthorized = Error.Problem(
            "StartupPartnerships.Unauthorized",
            "You are not allowed to perform this action on this startup partnership.");

        public static readonly Error DuplicateDomain = Error.Conflict(
            "StartupPartnerships.DuplicateDomain",
            "A partnership with this partner's website domain is already listed for this startup.");

        public static readonly Error LimitReached = Error.Problem(
            "StartupPartnerships.LimitReached",
            $"A startup can list at most {StartupPartnership.MaxPerStartup} partnerships.");

        public static readonly Error InvalidWebsite = Error.Validation(
            "StartupPartnerships.InvalidWebsite",
            "The partner website must be an absolute http(s) URL with a resolvable domain.");
    }
}
