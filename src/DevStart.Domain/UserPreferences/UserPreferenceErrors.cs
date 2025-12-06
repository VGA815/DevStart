using DevStart.SharedKernel;

namespace DevStart.Domain.UserPreferences
{
    public static class UserPreferenceErrors
    {
        public static Error NotFound(Guid userId) => Error.NotFound(
            "UserPreferences.NotFound",
            $"The user preferences with userId = '{userId}' was not found");
    }
}
