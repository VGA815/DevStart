using DevStart.SharedKernel;

namespace DevStart.Domain.ServiceOrders
{
    /// <summary>
    /// A one-time paid service purchased from the platform (SC-49): scoring report, term-sheet
    /// generation, promotion. Its own aggregate so a payment can exist without a subscription; the
    /// paid income still flows through the НПД income counter like any other payment.
    /// </summary>
    public sealed class ServiceOrder : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public ServiceType ServiceType { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public ServiceOrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? FulfilledAt { get; set; }

        public ServiceOrder() { }

        public static ServiceOrder CreatePending(
            Guid userId,
            ServiceType serviceType,
            decimal amount,
            string currency,
            DateTime utcNow)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ServiceType = serviceType,
                Amount = amount,
                Currency = currency,
                Status = ServiceOrderStatus.Pending,
                CreatedAt = utcNow,
            };

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
            Raise(new ServiceOrderPaidDomainEvent(Id, UserId, ServiceType));
            return Result.Success();
        }

        public Result MarkFulfilled(DateTime utcNow)
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
            return Result.Success();
        }

        public Result MarkCancelled(DateTime utcNow)
        {
            if (Status == ServiceOrderStatus.Cancelled)
            {
                return Result.Success();
            }
            Status = ServiceOrderStatus.Cancelled;
            return Result.Success();
        }

        public Result MarkRefunded(DateTime utcNow)
        {
            if (Status == ServiceOrderStatus.Refunded)
            {
                return Result.Success();
            }
            Status = ServiceOrderStatus.Refunded;
            return Result.Success();
        }
    }
}
