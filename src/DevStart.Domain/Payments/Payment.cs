using DevStart.SharedKernel;

namespace DevStart.Domain.Payments
{
    public sealed class Payment : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        /// <summary>Set for a subscription payment; <c>null</c> for a one-time service order.</summary>
        public Guid? SubscriptionId { get; set; }

        /// <summary>Set for a one-time service order; <c>null</c> for a subscription payment.</summary>
        public Guid? ServiceOrderId { get; set; }

        public PaymentPurpose Purpose { get; set; }
        public PaymentProvider Provider { get; set; }
        public string? ProviderPaymentId { get; set; }
        public string? ConfirmationUrl { get; set; }
        public decimal Amount { get; set; }
        public decimal RefundedAmount { get; set; }
        public string Currency { get; set; } = "RUB";
        public PaymentStatus Status { get; set; }
        public Guid? PromoCodeId { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public Payment() { }

        public static Payment CreatePending(
            Guid userId,
            Guid subscriptionId,
            PaymentProvider provider,
            decimal amount,
            string currency,
            DateTime utcNow,
            Guid? promoCodeId = null,
            decimal discountAmount = 0m)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionId = subscriptionId,
                ServiceOrderId = null,
                Purpose = PaymentPurpose.Subscription,
                Provider = provider,
                ProviderPaymentId = null,
                ConfirmationUrl = null,
                Amount = amount,
                RefundedAmount = 0m,
                Currency = currency,
                Status = PaymentStatus.Pending,
                PromoCodeId = promoCodeId,
                DiscountAmount = discountAmount,
                CreatedAt = utcNow,
                PaidAt = null,
            };

        public static Payment CreatePendingForServiceOrder(
            Guid userId,
            Guid serviceOrderId,
            PaymentProvider provider,
            decimal amount,
            string currency,
            DateTime utcNow)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionId = null,
                ServiceOrderId = serviceOrderId,
                Purpose = PaymentPurpose.ServiceOrder,
                Provider = provider,
                ProviderPaymentId = null,
                ConfirmationUrl = null,
                Amount = amount,
                RefundedAmount = 0m,
                Currency = currency,
                Status = PaymentStatus.Pending,
                PromoCodeId = null,
                DiscountAmount = 0m,
                CreatedAt = utcNow,
                PaidAt = null,
            };

        public void AssignProviderPayment(string providerPaymentId, string confirmationUrl)
        {
            ProviderPaymentId = providerPaymentId;
            ConfirmationUrl = confirmationUrl;
        }

        public Result MarkSucceeded(DateTime paidAt)
        {
            if (Status is PaymentStatus.Succeeded or PaymentStatus.Refunded)
            {
                return Result.Success();
            }
            if (Status is PaymentStatus.Cancelled or PaymentStatus.Failed)
            {
                return Result.Failure(PaymentErrors.ProviderError(
                    $"Cannot transition payment from {Status} to Succeeded."));
            }
            Status = PaymentStatus.Succeeded;
            PaidAt = paidAt;
            Raise(new PaymentSucceededDomainEvent(Id, UserId, Amount, paidAt));
            return Result.Success();
        }

        public Result MarkCancelled(DateTime utcNow)
        {
            if (Status == PaymentStatus.Cancelled)
            {
                return Result.Success();
            }
            Status = PaymentStatus.Cancelled;
            return Result.Success();
        }

        public Result MarkFailed(DateTime utcNow)
        {
            if (Status == PaymentStatus.Failed)
            {
                return Result.Success();
            }
            Status = PaymentStatus.Failed;
            return Result.Success();
        }

        /// <summary>
        /// Records the total amount refunded for this payment (cumulative, as reported by the
        /// provider). When the full amount has been refunded the payment becomes <see cref="PaymentStatus.Refunded"/>.
        /// Refunds only apply to a captured (Succeeded/Refunded) payment; otherwise this is a no-op.
        /// </summary>
        public Result MarkRefunded(decimal totalRefundedAmount, DateTime utcNow)
        {
            if (totalRefundedAmount < 0m)
            {
                return Result.Failure(PaymentErrors.ProviderError("Refunded amount cannot be negative."));
            }
            if (Status is not (PaymentStatus.Succeeded or PaymentStatus.Refunded))
            {
                return Result.Success();
            }

            RefundedAmount = totalRefundedAmount;
            if (totalRefundedAmount >= Amount && Status != PaymentStatus.Refunded)
            {
                Status = PaymentStatus.Refunded;
                // The refund event is subscription-oriented (drops the active-Pro cache, notifies the
                // user of deactivation). A service-order payment has no subscription; its order status
                // is handled by the sync/refund handlers directly, so no event is raised here.
                if (SubscriptionId is Guid subscriptionId)
                {
                    Raise(new PaymentRefundedDomainEvent(Id, UserId, subscriptionId, totalRefundedAmount));
                }
            }
            return Result.Success();
        }

        public bool IsFullyRefunded => RefundedAmount >= Amount && Amount > 0m;
    }
}
