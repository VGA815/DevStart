using DevStart.SharedKernel;

namespace DevStart.Domain.Profiles
{
    public static class ProfileErrors
    {
        public static Error NotFound(Guid userId) => Error.NotFound(
            "Profiles.NotFound",
            $"The profile with the userId = '{userId}' was not found.");
    }
}
