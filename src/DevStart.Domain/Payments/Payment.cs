using DevStart.SharedKernel;

namespace DevStart.Domain.Payments
{
    public sealed class Payment : Entity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid SubscriptionId { get; set; }
        public PaymentProvider Provider { get; set; }
        public string? ProviderPaymentId { get; set; }
        public string? ConfirmationUrl { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public PaymentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public Payment() { }

        public static Payment CreatePending(
            Guid userId,
            Guid subscriptionId,
            PaymentProvider provider,
            decimal amount,
            string currency,
            DateTime utcNow)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SubscriptionId = subscriptionId,
                Provider = provider,
                ProviderPaymentId = null,
                ConfirmationUrl = null,
                Amount = amount,
                Currency = currency,
                Status = PaymentStatus.Pending,
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
            if (Status == PaymentStatus.Succeeded)
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
    }
}
