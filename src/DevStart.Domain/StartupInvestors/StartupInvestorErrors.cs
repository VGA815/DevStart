using DevStart.SharedKernel;

namespace DevStart.Domain.StartupInvestors
{
    public static class StartupInvestorErrors
    {
        public static Error NotFound(Guid profileId, Guid startupId) => Error.NotFound(
            "StartupInvestors.NotFound",
            $"The startup investor with profileId = '{profileId}' and startupId = '{startupId}' was not found");
        public static readonly Error NotFoundByProfileId = Error.NotFound(
            "StartupInvestors.NotFoundByProfileId",
            "The startup investor with specified profileId was not found");
        public static readonly Error NotFoundByStartupId = Error.NotFound(
            "StartupInvestors.NotFoundByStartupId",
            "The startup investor with specified startupId was not found");
        public static readonly Error IsNotUnique = Error.Conflict(
            "StartupInvestors.IsNotUnique",
            "The specified profile is already investor of the specified startup");
    }
}
