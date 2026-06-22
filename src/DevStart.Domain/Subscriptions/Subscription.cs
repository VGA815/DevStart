using DevStart.SharedKernel;

namespace DevStart.Domain.Subscriptions
{
    public sealed class Subscription : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public SubscriptionPlan Plan { get; set; }
        public SubscriptionStatus Status { get; set; }
        public SubscriptionSource Source { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? RenewalReminderSentAt { get; set; }

        public Subscription() { }

        public static Subscription CreatePending(
            Guid userId,
            SubscriptionPlan plan,
            DateTime utcNow,
            SubscriptionSource source = SubscriptionSource.Purchase)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Plan = plan,
                Status = SubscriptionStatus.Pending,
                Source = source,
                StartedAt = utcNow,
                ExpiresAt = utcNow,
                CreatedAt = utcNow,
                UpdatedAt = utcNow,
                RenewalReminderSentAt = null,
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
            RenewalReminderSentAt = null;

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

        /// <summary>
        /// Moves an <see cref="SubscriptionStatus.Active"/> subscription whose term has ended to
        /// <see cref="SubscriptionStatus.Expired"/>. No-op for any other status.
        /// </summary>
        public Result MarkExpired(DateTime utcNow)
        {
            if (Status != SubscriptionStatus.Active)
            {
                return Result.Success();
            }
            Status = SubscriptionStatus.Expired;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        /// <summary>
        /// Extends an active subscription's term by <paramref name="additionalDays"/>. The new term is
        /// measured from the later of the current expiry and <paramref name="utcNow"/>, so an already-lapsed
        /// <see cref="ExpiresAt"/> (clock skew, delayed transitions, data fixes) doesn't yield a still-past
        /// expiry. Clears any previously-sent renewal reminder so a fresh one can be issued.
        /// </summary>
        public Result Extend(int additionalDays, DateTime utcNow)
        {
            if (additionalDays <= 0)
            {
                return Result.Failure(SubscriptionErrors.CannotExtend);
            }
            if (Status != SubscriptionStatus.Active)
            {
                return Result.Failure(SubscriptionErrors.CannotExtend);
            }

            DateTime basis = ExpiresAt > utcNow ? ExpiresAt : utcNow;
            ExpiresAt = basis.AddDays(additionalDays);
            UpdatedAt = utcNow;
            RenewalReminderSentAt = null;
            return Result.Success();
        }

        public void MarkRenewalReminderSent(DateTime utcNow)
        {
            RenewalReminderSentAt = utcNow;
            UpdatedAt = utcNow;
        }

        public bool IsActiveAt(DateTime utcNow) =>
            Status == SubscriptionStatus.Active && ExpiresAt > utcNow;
    }
}
