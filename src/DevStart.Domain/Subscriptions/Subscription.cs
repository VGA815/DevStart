using DevStart.SharedKernel;

namespace DevStart.Domain.Subscriptions
{
    public sealed class Subscription : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public SubscriptionPlan Plan { get; set; }
        public SubscriptionStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Subscription() { }

        public static Subscription CreatePending(Guid userId, SubscriptionPlan plan, DateTime utcNow)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Plan = plan,
                Status = SubscriptionStatus.Pending,
                StartedAt = utcNow,
                ExpiresAt = utcNow,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
            };

        public Result Activate(DateTime utcNow, int durationDays)
        {
            if (Status == SubscriptionStatus.Active)
            {
                // Idempotent re-activation: ignore.
                return Result.Success();
            }
            if (Status != SubscriptionStatus.Pending)
            {
                return Result.Failure(SubscriptionErrors.WrongStatusForActivation);
            }

            Status = SubscriptionStatus.Active;
            StartedAt = utcNow;
            ExpiresAt = utcNow.AddDays(durationDays);
            UpdatedAt = utcNow;

            Raise(new SubscriptionActivatedDomainEvent(Id, UserId, Plan, ExpiresAt));
            return Result.Success();
        }

        public Result MarkCancelled(DateTime utcNow)
        {
            if (Status == SubscriptionStatus.Cancelled)
            {
                return Result.Success();
            }
            Status = SubscriptionStatus.Cancelled;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        public bool IsActiveAt(DateTime utcNow) =>
            Status == SubscriptionStatus.Active && ExpiresAt > utcNow;
    }
}
