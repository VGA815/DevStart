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

        public static readonly Error ExpertProfileIdRequired = Error.Problem(
            "ExpertCollaborationRequests.ExpertProfileIdRequired",
            "ExpertProfileId is required when a startup invites an expert.");

        public static readonly Error ExpertProfileNotFound = Error.Problem(
            "ExpertCollaborationRequests.ExpertProfileNotFound",
            "The invited expert does not have an expert profile.");

        public static readonly Error ExpertAlreadyMember = Error.Problem(
            "ExpertCollaborationRequests.ExpertAlreadyMember",
            "This expert is already a member of the startup.");

        public static readonly Error StartupUnavailable = Error.Problem(
            "ExpertCollaborationRequests.StartupUnavailable",
            "This startup is not available for collaboration requests.");

        // ProposedHoursPerWeek / ProposedRate ranges are enforced by
        // CreateExpertCollaborationRequestCommandValidator, which short-circuits ahead of the handler.

        public static readonly Error AlreadyExistsForStartup = Error.Conflict(
            "ExpertCollaborationRequests.AlreadyExistsForStartup",
            "A pending collaboration request between you and this startup already exists.");

        public static Error RejectionCooldownActive(DateTime retryAfterUtc) => Error.Conflict(
            "ExpertCollaborationRequests.RejectionCooldownActive",
            $"Your previous request was rejected. You can send a new one after {retryAfterUtc:yyyy-MM-dd}.");
    }
}
