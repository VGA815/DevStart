using DevStart.SharedKernel;

namespace DevStart.Domain.ExpertCollaborationRequests
{
    public static class ExpertCollaborationRequestErrors
    {
        public static Error NotFound(Guid requestId) => Error.NotFound(
            "ExpertCollaborationRequests.NotFound",
            $"The expert collaboration request with id = '{requestId}' was not found.");

        public static readonly Error Unauthorized = Error.Problem(
            "ExpertCollaborationRequests.Unauthorized",
            "You are not allowed to perform this action on this expert collaboration request.");

        public static readonly Error MustBePending = Error.Problem(
            "ExpertCollaborationRequests.MustBePending",
            "The expert collaboration request must be in Pending status to perform this action.");

        public static readonly Error CannotApplyToOwnStartup = Error.Problem(
            "ExpertCollaborationRequests.CannotApplyToOwnStartup",
            "You cannot send a collaboration request to a startup you are a member of.");

        public static readonly Error ExpertProfileRequired = Error.Problem(
            "ExpertCollaborationRequests.ExpertProfileRequired",
            "You must have an expert profile to create a collaboration request.");

        public static readonly Error InvalidProposedHours = Error.Problem(
            "ExpertCollaborationRequests.InvalidProposedHours",
            "ProposedHoursPerWeek must be between 1 and 168 when specified.");

        public static readonly Error InvalidProposedRate = Error.Problem(
            "ExpertCollaborationRequests.InvalidProposedRate",
            "ProposedRate must be greater than zero when specified.");

        public static readonly Error AlreadyExistsForStartup = Error.Conflict(
            "ExpertCollaborationRequests.AlreadyExistsForStartup",
            "A pending collaboration request from you to this startup already exists.");
    }
}
