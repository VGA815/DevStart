using DevStart.SharedKernel;

namespace DevStart.Domain.StartupEquity
{
    public static class StartupEquityErrors
    {
        public static readonly Error Unauthorized = Error.Forbidden(
            "StartupEquity.Unauthorized",
            "Only a founder or administrator of the startup may view or edit its cap table.");

        public static readonly Error PercentagesMustSumTo100 = Error.Validation(
            "StartupEquity.PercentagesMustSumTo100",
            "The equity percentages of all holders must sum to exactly 100%.");

        public static readonly Error InvalidPercentage = Error.Validation(
            "StartupEquity.InvalidPercentage",
            "Each holder's equity percentage must be between 0 and 100.");

        public static readonly Error InvalidVesting = Error.Validation(
            "StartupEquity.InvalidVesting",
            "Vesting is inconsistent: a schedule needs a start date and a positive duration, and the cliff must not exceed the duration.");

        public static readonly Error FounderNotAMember = Error.Validation(
            "StartupEquity.FounderNotAMember",
            "Every founder row must reference a profile that is a founder member of the startup.");

        public static readonly Error DuplicateFounder = Error.Validation(
            "StartupEquity.DuplicateFounder",
            "A profile may appear at most once on the cap table.");

        public static Error StartupNotFound(Guid startupId) => Error.NotFound(
            "StartupEquity.StartupNotFound",
            $"The startup with id = '{startupId}' was not found.");
    }
}
