namespace DevStart.Domain.Payments
{
    /// <summary>What a <see cref="Payment"/> pays for: a subscription, or a one-time service order.</summary>
    public enum PaymentPurpose
    {
        Subscription = 0,
        ServiceOrder = 1,
    }
}
