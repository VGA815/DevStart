using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Subscriptions;

namespace DevStart.Application.Admin.Subscriptions.GetSubscriptions
{
    public sealed record GetAdminSubscriptionsQuery(
        Guid? UserId = null,
        SubscriptionStatus? Status = null,
        SubscriptionPlan? Plan = null,
        int PageNumber = 1,
        int PageSize = 50) : IQuery<List<AdminSubscriptionResponse>>;

    public sealed class AdminSubscriptionResponse
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string? UserEmail { get; init; }
        public SubscriptionPlan Plan { get; init; }
        public SubscriptionStatus Status { get; init; }
        public SubscriptionSource Source { get; init; }
        public DateTime StartedAt { get; init; }
        public DateTime ExpiresAt { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
