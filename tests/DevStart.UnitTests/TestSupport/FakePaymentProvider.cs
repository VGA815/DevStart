using DevStart.Application.Abstractions.Payments;

namespace DevStart.UnitTests.TestSupport
{
    internal sealed class FakePaymentProvider : IPaymentProvider
    {
        public ProviderPaymentSnapshot? SnapshotToReturn { get; set; }
        public CreatedPayment CreatedToReturn { get; set; } = new("provider-pay-1", "https://pay.example/redirect");
        public string RefundIdToReturn { get; set; } = "refund-1";
        public bool RefundSucceededToReturn { get; set; } = true;
        public PaymentWebhookEvent? WebhookToReturn { get; set; }

        // When set, the corresponding operation throws instead of returning — used to simulate a
        // provider outage or rejection so handlers can be verified to translate it into a Result.
        public Exception? CreatePaymentException { get; set; }
        public Exception? CreateRefundException { get; set; }

        public CreatePaymentInput? LastCreateInput { get; private set; }
        public CreateRefundInput? LastRefundInput { get; private set; }
        public string? LastGetId { get; private set; }

        public Task<CreatedPayment> CreatePaymentAsync(CreatePaymentInput input, CancellationToken ct)
        {
            LastCreateInput = input;
            if (CreatePaymentException is not null)
            {
                throw CreatePaymentException;
            }
            return Task.FromResult(CreatedToReturn);
        }

        public Task<ProviderPaymentSnapshot?> GetPaymentAsync(string providerPaymentId, CancellationToken ct)
        {
            LastGetId = providerPaymentId;
            return Task.FromResult(SnapshotToReturn);
        }

        public Task<CreatedRefund> CreateRefundAsync(CreateRefundInput input, CancellationToken ct)
        {
            LastRefundInput = input;
            if (CreateRefundException is not null)
            {
                throw CreateRefundException;
            }
            return Task.FromResult(new CreatedRefund(RefundIdToReturn, RefundSucceededToReturn));
        }

        public PaymentWebhookEvent? ParseWebhook(string body) => WebhookToReturn;
    }
}
