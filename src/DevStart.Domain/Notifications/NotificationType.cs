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
    }
}
