using DevStart.SharedKernel;

namespace DevStart.Domain.StartupMembers
{
    public static class StartupMemberErrors
    {
        public static Error NotFound(Guid profileId, Guid startupId) => Error.NotFound(
            "StartupMembers.NotFound",
            $"The startup member with profileId = '{profileId}' and startupId = '{startupId}' was not found");
        public static readonly Error NotFoundByProfileId = Error.NotFound(
            "StartupMembers.NotFoundByProfileId",
            "The startup member with specified profileId was not found");
        public static readonly Error NotFoundByStartupId = Error.NotFound(
            "StartupMembers.NotFoundByStartupId",
            "The startup member with specified startupId was not found");
        public static readonly Error IsNotUnique = Error.Conflict(
            "Startupmembers.IsNotUnique",
            "The specified profile is already member of specified startup");
    }
}
