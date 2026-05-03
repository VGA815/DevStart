using DevStart.SharedKernel;

namespace DevStart.Domain.InvestmentApplications
{
    public static class InvestmentApplicationErrors
    {
        public static Error NotFound(Guid applicationId) => Error.NotFound(
            "InvestmentApplications.NotFound",
            $"The investment application with id = '{applicationId}' was not found.");

        public static readonly Error Unauthorized = Error.Problem(
            "InvestmentApplications.Unauthorized",
            "You are not allowed to perform this action on this investment application.");

        public static readonly Error MustBePending = Error.Problem(
            "InvestmentApplications.MustBePending",
            "The investment application must be in Pending status to perform this action.");

        public static readonly Error CannotApplyToOwnStartup = Error.Problem(
            "InvestmentApplications.CannotApplyToOwnStartup",
            "You cannot apply to a startup you are a member of.");

        public static readonly Error RoadmapItemNotFound = Error.NotFound(
            "InvestmentApplications.RoadmapItemNotFound",
            "The referenced roadmap item was not found or does not belong to the specified startup.");

        public static readonly Error InvalidAmount = Error.Problem(
            "InvestmentApplications.InvalidAmount",
            "The investment amount must be greater than zero.");

        public static readonly Error InvestorProfileRequired = Error.Problem(
            "InvestmentApplications.InvestorProfileRequired",
            "You must have an investor profile to create an investment application.");

        public static readonly Error InstrumentRequired = Error.Problem(
            "InvestmentApplications.InstrumentRequired",
            "Investment instrument must be specified.");

        public static readonly Error InvalidValuationCap = Error.Problem(
            "InvestmentApplications.InvalidValuationCap",
            "ValuationCap must be greater than zero for SAFE and Convertible Loan instruments.");

        public static readonly Error InvalidDiscount = Error.Problem(
            "InvestmentApplications.InvalidDiscount",
            "Discount must be between 0 and 0.5 (0–50%).");

        public static readonly Error InvalidInterestRate = Error.Problem(
            "InvestmentApplications.InvalidInterestRate",
            "InterestRate must be between 0 and 0.30 (0–30%) and is required for Convertible Loan.");

        public static readonly Error InvalidTermMonths = Error.Problem(
            "InvestmentApplications.InvalidTermMonths",
            "TermMonths must be between 6 and 60 and is required for Convertible Loan.");

        public static readonly Error InvalidPreMoneyValuation = Error.Problem(
            "InvestmentApplications.InvalidPreMoneyValuation",
            "PreMoneyValuation must be greater than zero and is required for Priced Round.");

        public static readonly Error InvalidLiquidationPreference = Error.Problem(
            "InvestmentApplications.InvalidLiquidationPreference",
            "LiquidationPreference must be between 1.0 and 3.0.");

        public static readonly Error InconsistentTerms = Error.Problem(
            "InvestmentApplications.InconsistentTerms",
            "Provided deal terms are not consistent with the selected instrument.");
    }
}
