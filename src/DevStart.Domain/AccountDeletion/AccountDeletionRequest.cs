using DevStart.SharedKernel;

namespace DevStart.Domain.AccountDeletion
{
    /// <summary>
    /// A user's request to have their account erased (ст. 21 ФЗ-152, offer §8.2).
    ///
    /// Erasure is deferred rather than immediate: <see cref="ScheduledFor"/> gives the user a window to
    /// change their mind (and an accidental or hijacked click a window to be noticed), after which a
    /// daily job erases the account. The window is deliberately far shorter than the 30 days the legal
    /// documents promise, so a late job run still lands well inside the promise.
    ///
    /// The row survives the user it points at: once <see cref="AccountDeletionRequestStatus.Completed"/>
    /// it holds nothing but ids and timestamps, and it is the only evidence left that the erasure was
    /// carried out on time.
    /// </summary>
    public sealed class AccountDeletionRequest : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime RequestedAt { get; set; }

        /// <summary>When the grace window closes and the account becomes eligible for erasure.</summary>
        public DateTime ScheduledFor { get; set; }

        public AccountDeletionRequestStatus Status { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public bool IsPending => Status == AccountDeletionRequestStatus.Pending;

        public bool IsDue(DateTime utcNow) => IsPending && ScheduledFor <= utcNow;

        public AccountDeletionRequest()
        {
        }

        public static AccountDeletionRequest Create(Guid userId, DateTime utcNow, TimeSpan grace)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RequestedAt = utcNow,
                ScheduledFor = utcNow + grace,
                Status = AccountDeletionRequestStatus.Pending,
            };

        public Result Cancel(DateTime utcNow)
        {
            if (!IsPending)
            {
                return Result.Failure(AccountDeletionErrors.NotPending);
            }

            Status = AccountDeletionRequestStatus.Cancelled;
            CancelledAt = utcNow;
            return Result.Success();
        }

        public void Complete(DateTime utcNow)
        {
            Status = AccountDeletionRequestStatus.Completed;
            CompletedAt = utcNow;
        }
    }
}
