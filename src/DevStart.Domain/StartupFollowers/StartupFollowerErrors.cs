using DevStart.SharedKernel;

namespace DevStart.Domain.StartupFollowers
{
    public static class StartupFollowerErrors
    {
        public static Error NotFound(Guid startupId, Guid profileId) => Error.NotFound(
            "StartupFollowers.NotFound",
            $"The startup follower with startupId = '{startupId}' and profileId = '{profileId}' was not found");
        public static readonly Error NotFoundByStartupId = Error.NotFound(
            "StartupFollowers.NotFoundByStartupId",
            "The startup follower with specified startupId was not found");
        public static readonly Error NotFoundByProfileId = Error.NotFound(
            "StartupFollowers.NotFoundByProfileId",
            "The startup follower with specified profileId was not found");
        public static readonly Error IsNotUnique = Error.Conflict(
            "StartupFollowers.IsNotUnique",
            "The specified profile already follow specified startup");
    }
}
