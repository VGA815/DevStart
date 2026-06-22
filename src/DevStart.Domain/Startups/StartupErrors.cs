using DevStart.SharedKernel;

namespace DevStart.Domain.Startups
{
    public static class StartupErrors
    {
        public static Error NotFound(Guid startupId) => Error.NotFound(
            "Startups.NotFound",
            $"The startup with the Id = '{startupId}' was not found");
        public static readonly Error NotFoundByName = Error.NotFound(
            "Startups.NotFoundByName",
            "The startup with the specified name was not found");
        public static readonly Error NameNotUnique = Error.Conflict(
            "Startups.NameNotUnique",
            "The provided name is not unique");
        public static readonly Error UserAlreadyMember = Error.Conflict(
            "Startups.UserAlreadyMember",
            "The user is already a member of the startup");
        public static readonly Error AlreadyBanned = Error.Conflict(
            "Startups.AlreadyBanned",
            "The startup is already banned");
        public static readonly Error NotBanned = Error.Conflict(
            "Startups.NotBanned",
            "The startup is not banned");
        public static readonly Error BanExpiryInPast = Error.Validation(
            "Startups.BanExpiryInPast",
            "The ban expiry date must be in the future");
    }
}
