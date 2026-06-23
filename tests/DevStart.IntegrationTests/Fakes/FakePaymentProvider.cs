using DevStart.Application.Abstractions.Payments;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>
    /// Controllable <see cref="IPaymentProvider"/> replacing YooKassa. Returns canned values so checkout
    /// flows complete without touching the network; the captured inputs let tests assert what would have
    /// been sent (amount, customer email, idempotence key), and the exception hooks simulate provider
    /// outages/rejections so handlers can be checked for translating them into a <c>Result</c>.
    /// </summary>
    internal sealed class FakePaymentProvider : IPaymentProvider
    {
        public CreatedPayment CreatedToReturn { get; set; } = new("provider-pay-1", "https://pay.test.local/redirect");
        public ProviderPaymentSnapshot? SnapshotToReturn { get; set; }
        public string RefundIdToReturn { get; set; } = "refund-1";
        public bool RefundSucceededToReturn { get; set; } = true;
        public PaymentWebhookEvent? WebhookToReturn { get; set; }

        public Exception? CreatePaymentException { get; set; }
        public Exception? CreateRefundException { get; set; }

        public CreatePaymentInput? LastCreateInput { get; private set; }
        public CreateRefundInput? LastRefundInput { get; private set; }

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
            => Task.FromResult(SnapshotToReturn);

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
