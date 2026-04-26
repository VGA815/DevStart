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
    }
}
