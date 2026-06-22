namespace DevStart.Domain.Admin
{
    public enum AdminActionType
    {
        BanUser = 0,
        UnbanUser = 1,
        BanStartup = 2,
        UnbanStartup = 3,
        GrantSubscription = 4,
        ExtendSubscription = 5,
        RevokeSubscription = 6,
        CreatePromoCode = 7,
        DeactivatePromoCode = 8,
    }
}
