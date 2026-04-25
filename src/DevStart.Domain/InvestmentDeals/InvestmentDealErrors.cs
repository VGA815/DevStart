using DevStart.SharedKernel;

namespace DevStart.Domain.InvestmentDeals
{
    public static class InvestmentDealErrors
    {
        public static Error NotFound(Guid dealId) => Error.NotFound(
            "InvestmentDeals.NotFound",
            $"The investment deal with id = '{dealId}' was not found.");

        public static readonly Error Unauthorized = Error.Problem(
            "InvestmentDeals.Unauthorized",
            "You are not allowed to perform this action on this investment deal.");

        public static readonly Error AlreadyCompleted = Error.Problem(
            "InvestmentDeals.AlreadyCompleted",
            "The investment deal is already completed.");

        public static readonly Error AlreadyCancelled = Error.Problem(
            "InvestmentDeals.AlreadyCancelled",
            "The investment deal is already cancelled.");

        public static readonly Error AlreadyConfirmed = Error.Problem(
            "InvestmentDeals.AlreadyConfirmed",
            "You have already confirmed this investment deal.");
    }
}
