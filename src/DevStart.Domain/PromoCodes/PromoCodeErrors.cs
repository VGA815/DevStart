using DevStart.SharedKernel;

namespace DevStart.Domain.PromoCodes
{
    public static class PromoCodeErrors
    {
        public static Error NotFound(Guid promoCodeId) => Error.NotFound(
            "PromoCodes.NotFound",
            $"The promo code with id = '{promoCodeId}' was not found.");

        public static readonly Error InvalidCode = Error.Validation(
            "PromoCodes.InvalidCode",
            "The promo code is invalid.");

        public static readonly Error CodeAlreadyExists = Error.Conflict(
            "PromoCodes.CodeAlreadyExists",
            "A promo code with this code already exists.");

        public static readonly Error Inactive = Error.Validation(
            "PromoCodes.Inactive",
            "This promo code is no longer active.");

        public static readonly Error NotYetValid = Error.Validation(
            "PromoCodes.NotYetValid",
            "This promo code is not valid yet.");

        public static readonly Error Expired = Error.Validation(
            "PromoCodes.Expired",
            "This promo code has expired.");

        public static readonly Error GlobalLimitReached = Error.Conflict(
            "PromoCodes.GlobalLimitReached",
            "This promo code has reached its redemption limit.");

        public static readonly Error AlreadyRedeemedByUser = Error.Conflict(
            "PromoCodes.AlreadyRedeemedByUser",
            "You have already used this promo code.");

        public static readonly Error PlanMismatch = Error.Validation(
            "PromoCodes.PlanMismatch",
            "This promo code does not apply to the selected plan.");
    }
}
