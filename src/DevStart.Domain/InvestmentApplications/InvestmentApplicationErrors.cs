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
    }
}
