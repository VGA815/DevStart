namespace DevStart.Domain.Notifications
{
    public enum NotificationType
    {
        Welcome = 0,
        EmailVerified = 1,
        MessageReceived = 2,
        StartupMemberAdded = 3,
        InvestmentApplicationReceived = 4,
        InvestmentApplicationAccepted = 5,
        InvestmentApplicationRejected = 6,
        InvestmentApplicationWithdrawn = 7,
        InvestmentDealCompleted = 8,
        DealDocumentsReady = 9,
        SubscriptionActivated = 10,
        ExpertCollaborationRequestReceived = 11,
        ExpertCollaborationRequestAccepted = 12,
        ExpertCollaborationRequestRejected = 13,
        ExpertCollaborationRequestWithdrawn = 14,
        SubscriptionExpiringSoon = 15,
        SubscriptionExpired = 16,
        PaymentRefunded = 17,
        CommunityStandardsIncomplete = 18,
        IncomeLimitWarning = 19,
        ServiceOrderFulfilled = 20,

        // 11–14 and 21 cover requests an expert opened, so they always land on the startup side for
        // "received/withdrawn" and on the expert side for the answers. 22–26 are their mirror for
        // invitations a startup opened. Splitting them keeps the recipient's side derivable from the
        // type alone, which is what the client routes on.
        ExpertCollaborationRequestExpired = 21,
        ExpertCollaborationInvitationReceived = 22,
        ExpertCollaborationInvitationWithdrawn = 23,
        ExpertCollaborationInvitationAccepted = 24,
        ExpertCollaborationInvitationRejected = 25,
        ExpertCollaborationInvitationExpired = 26,
    }
}
