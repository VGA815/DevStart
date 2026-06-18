using DevStart.SharedKernel;

namespace DevStart.Domain.Investors
{
    public static class InvestorProfileErrors
    {
        public static Error NotFound(Guid userId) => Error.NotFound(
            "InvestorProfiles.NotFound",
            $"The investor profile with userId = '{userId}' was not found.");

        public static Error AlreadyExists(Guid userId) => Error.Conflict(
            "InvestorProfiles.AlreadyExists",
            $"The investor profile with userId = '{userId}' already exists.");

        public static readonly Error Unauthorized = Error.Problem(
            "InvestorProfiles.Unauthorized",
            "You are not allowed to perform this action on this investor profile.");

        public static readonly Error ProfileNameRequired = Error.Problem(
            "InvestorProfiles.ProfileNameRequired",
            "A profile with a non-empty name is required before creating an investor profile.");
    }
}
