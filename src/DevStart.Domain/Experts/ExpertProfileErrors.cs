using DevStart.SharedKernel;

namespace DevStart.Domain.Experts
{
    public static class ExpertProfileErrors
    {
        public static Error NotFound(Guid userId) => Error.NotFound(
            "ExpertProfiles.NotFound",
            $"The expert profile with userId = '{userId}' was not found.");

        public static Error AlreadyExists(Guid userId) => Error.Conflict(
            "ExpertProfiles.AlreadyExists",
            $"The expert profile with userId = '{userId}' already exists.");

        public static readonly Error Unauthorized = Error.Problem(
            "ExpertProfiles.Unauthorized",
            "You are not allowed to perform this action on this expert profile.");
    }
}
