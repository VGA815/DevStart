using DevStart.SharedKernel;

namespace DevStart.Domain.StartupCompetitors
{
    public static class StartupCompetitorErrors
    {
        public static Error NotFound(Guid competitorId) => Error.NotFound(
            "StartupCompetitors.NotFound",
            $"The startup competitor with id = '{competitorId}' was not found.");

        public static readonly Error Unauthorized = Error.Problem(
            "StartupCompetitors.Unauthorized",
            "You are not allowed to perform this action on this startup competitor.");

        public static readonly Error DuplicateDomain = Error.Conflict(
            "StartupCompetitors.DuplicateDomain",
            "A competitor with this website domain is already listed for this startup.");

        public static readonly Error LimitReached = Error.Problem(
            "StartupCompetitors.LimitReached",
            $"A startup can list at most {StartupCompetitor.MaxPerStartup} competitors.");

        public static readonly Error InvalidWebsite = Error.Validation(
            "StartupCompetitors.InvalidWebsite",
            "The competitor website must be an absolute http(s) URL with a resolvable domain.");
    }
}
