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
    }
}
