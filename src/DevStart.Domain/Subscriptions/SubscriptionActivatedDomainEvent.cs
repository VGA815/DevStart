using DevStart.SharedKernel;

namespace DevStart.Domain.Subscriptions
{
    public sealed record SubscriptionActivatedDomainEvent(
        Guid SubscriptionId,
        Guid UserId,
        SubscriptionPlan Plan,
        DateTime ExpiresAt) : IDomainEvent;
}
