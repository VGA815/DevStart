using DevStart.SharedKernel;

namespace DevStart.Domain.Profiles
{
    public static class ProfileErrors
    {
        public static Error NotFound(Guid userId) => Error.NotFound(
            "Profiles.NotFound",
            $"The profile with the userId = '{userId}' was not found.");
        public static readonly Error NotUnique = Error.Conflict(
            "Profiles.NotUnique",
            "The profile is already exists.");
    }
}
