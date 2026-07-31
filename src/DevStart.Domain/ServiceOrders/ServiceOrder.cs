using DevStart.SharedKernel;

namespace DevStart.Domain.ServiceOrders
{
    /// <summary>
    /// A one-time paid service purchased from the platform (SC-49): scoring report, term-sheet
    /// generation, promotion. Its own aggregate so a payment can exist without a subscription; the
    /// paid income still flows through the НПД income counter like any other payment.
    /// <para>
    /// A fulfilled order is also the entitlement record: it names the target it was bought for and,
    /// for time-boxed services, when access ends. Nothing else has to be written for the buyer to get
    /// what they paid for.
    /// </para>
    /// </summary>
    public sealed class ServiceOrder : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public ServiceType ServiceType { get; set; }

        /// <summary>The startup or deal this service was bought for. Null only for targetless services.</summary>
        public Guid? TargetId { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public ServiceOrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? FulfilledAt { get; set; }

        /// <summary>When the granted access ends. Null means the delivery is permanent.</summary>
        public DateTime? ExpiresAt { get; set; }

        public DateTime? CancelledAt { get; set; }
        public DateTime? RefundedAt { get; set; }

        public ServiceOrder() { }

        public static ServiceOrder CreatePending(
            Guid userId,
            ServiceType serviceType,
            Guid? targetId,
            decimal amount,
            string currency,
            DateTime utcNow)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ServiceType = serviceType,
                TargetId = targetId,
                Amount = amount,
                Currency = currency,
                Status = ServiceOrderStatus.Pending,
                CreatedAt = utcNow,
            };

        /// <summary>
        /// Whether this order currently entitles its buyer to the service. Expiry is "lazy" — the same
        /// approach as <see cref="Startups.Startup.IsCurrentlyBanned"/> — so no background job has to
        /// sweep expired access.
        /// </summary>
        public bool GrantsAccess(DateTime utcNow)
            => Status == ServiceOrderStatus.Fulfilled && (ExpiresAt is null || ExpiresAt > utcNow);

        public Result MarkPaid(DateTime utcNow)
        {
            if (Status is ServiceOrderStatus.Paid or ServiceOrderStatus.Fulfilled)
            {
                return Result.Success();
            }
            if (Status is ServiceOrderStatus.Cancelled or ServiceOrderStatus.Refunded)
            {
                return Result.Failure(ServiceOrderErrors.NotPayable);
            }

            Status = ServiceOrderStatus.Paid;
            PaidAt = utcNow;
            Raise(new ServiceOrderPaidDomainEvent(Id, UserId, ServiceType, TargetId));
            return Result.Success();
        }

        /// <param name="accessDays">Length of the granted access window; 0 means permanent.</param>
        public Result MarkFulfilled(DateTime utcNow, int accessDays)
        {
            if (Status == ServiceOrderStatus.Fulfilled)
            {
                return Result.Success();
            }
            if (Status != ServiceOrderStatus.Paid)
            {
                return Result.Failure(ServiceOrderErrors.NotFulfillable);
            }

            Status = ServiceOrderStatus.Fulfilled;
            FulfilledAt = utcNow;
            ExpiresAt = accessDays > 0 ? utcNow.AddDays(accessDays) : null;
            return Result.Success();
        }

        public Result MarkCancelled(DateTime utcNow)
        {
            if (Status == ServiceOrderStatus.Cancelled)
            {
                return Result.Success();
            }
            if (Status == ServiceOrderStatus.Refunded)
            {
                return Result.Failure(ServiceOrderErrors.NotCancellable);
            }

            Status = ServiceOrderStatus.Cancelled;
            CancelledAt = utcNow;
            // Cancelling revokes whatever was granted: GrantsAccess only answers true for Fulfilled.
            ExpiresAt = null;
            return Result.Success();
        }

        public Result MarkRefunded(DateTime utcNow)
        {
            if (Status == ServiceOrderStatus.Refunded)
            {
                return Result.Success();
            }

            Status = ServiceOrderStatus.Refunded;
            RefundedAt = utcNow;
            ExpiresAt = null;
            return Result.Success();
        }
    }
}
