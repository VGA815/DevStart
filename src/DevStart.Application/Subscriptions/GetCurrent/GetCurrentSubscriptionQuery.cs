using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Subscriptions;

namespace DevStart.Application.Subscriptions.GetCurrent
{
    public sealed record GetCurrentSubscriptionQuery() : IQuery<CurrentSubscriptionResponse>;

    public sealed class CurrentSubscriptionResponse
    {
        public Guid? SubscriptionId { get; init; }
        public SubscriptionPlan Plan { get; init; }
        public SubscriptionStatus? Status { get; init; }
        public DateTime? StartedAt { get; init; }
        public DateTime? ExpiresAt { get; init; }
        public bool IsActivePro { get; init; }
    }
}
