namespace DevStart.Domain.Subscriptions
{
    /// <summary>
    /// How a subscription came to exist: a paid checkout, an admin-granted comp, or a promo code.
    /// </summary>
    public enum SubscriptionSource
    {
        Purchase = 0,
        AdminGrant = 1,
        Promo = 2,
    }
}
