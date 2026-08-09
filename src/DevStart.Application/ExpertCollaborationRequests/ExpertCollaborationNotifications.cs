using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.Domain.Notifications;

namespace DevStart.Application.ExpertCollaborationRequests
{
    /// <summary>
    /// Picks the notification type and copy for each request lifecycle event. The type differs per
    /// direction on purpose: the client routes a notification to the startup dashboard or the expert
    /// dashboard from the type alone, and which side receives an event flips with the initiator.
    /// </summary>
    internal static class ExpertCollaborationNotifications
    {
        public static (NotificationType Type, string Title, string Body) Received(CollaborationRequestInitiator initiator)
            => initiator == CollaborationRequestInitiator.Expert
                ? (NotificationType.ExpertCollaborationRequestReceived,
                   "New expert collaboration request",
                   "You have received a new collaboration request from an expert.")
                : (NotificationType.ExpertCollaborationInvitationReceived,
                   "New collaboration invitation",
                   "A startup has invited you to collaborate.");

        public static (NotificationType Type, string Title, string Body) Accepted(CollaborationRequestInitiator initiator)
            => initiator == CollaborationRequestInitiator.Expert
                ? (NotificationType.ExpertCollaborationRequestAccepted,
                   "Collaboration request accepted",
                   "Your collaboration request has been accepted.")
                : (NotificationType.ExpertCollaborationInvitationAccepted,
                   "Collaboration invitation accepted",
                   "The expert accepted your collaboration invitation.");

        public static (NotificationType Type, string Title, string Body) Rejected(CollaborationRequestInitiator initiator)
            => initiator == CollaborationRequestInitiator.Expert
                ? (NotificationType.ExpertCollaborationRequestRejected,
                   "Collaboration request rejected",
                   "Your collaboration request has been rejected.")
                : (NotificationType.ExpertCollaborationInvitationRejected,
                   "Collaboration invitation declined",
                   "The expert declined your collaboration invitation.");

        public static (NotificationType Type, string Title, string Body) Withdrawn(CollaborationRequestInitiator initiator)
            => initiator == CollaborationRequestInitiator.Expert
                ? (NotificationType.ExpertCollaborationRequestWithdrawn,
                   "Collaboration request withdrawn",
                   "An expert has withdrawn their collaboration request.")
                : (NotificationType.ExpertCollaborationInvitationWithdrawn,
                   "Collaboration invitation withdrawn",
                   "A startup has withdrawn its collaboration invitation.");

        public static (NotificationType Type, string Title, string Body) Expired(CollaborationRequestInitiator initiator)
            => initiator == CollaborationRequestInitiator.Expert
                ? (NotificationType.ExpertCollaborationRequestExpired,
                   "Collaboration request expired",
                   "Your collaboration request expired without an answer. You can send a new one.")
                : (NotificationType.ExpertCollaborationInvitationExpired,
                   "Collaboration invitation expired",
                   "Your collaboration invitation expired without an answer. You can send a new one.");
    }
}
