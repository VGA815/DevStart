using DevStart.Application.Abstractions.Payments;

namespace DevStart.UnitTests.TestSupport
{
    internal sealed class FakePaymentProvider : IPaymentProvider
    {
        public ProviderPaymentSnapshot? SnapshotToReturn { get; set; }
        public CreatedPayment CreatedToReturn { get; set; } = new("provider-pay-1", "https://pay.example/redirect");
        public string RefundIdToReturn { get; set; } = "refund-1";
        public PaymentWebhookEvent? WebhookToReturn { get; set; }

        public CreatePaymentInput? LastCreateInput { get; private set; }
        public CreateRefundInput? LastRefundInput { get; private set; }
        public string? LastGetId { get; private set; }

        public Task<CreatedPayment> CreatePaymentAsync(CreatePaymentInput input, CancellationToken ct)
        {
            LastCreateInput = input;
            return Task.FromResult(CreatedToReturn);
        }

        public Task<ProviderPaymentSnapshot?> GetPaymentAsync(string providerPaymentId, CancellationToken ct)
        {
            LastGetId = providerPaymentId;
            return Task.FromResult(SnapshotToReturn);
        }

        public Task<string> CreateRefundAsync(CreateRefundInput input, CancellationToken ct)
        {
            LastRefundInput = input;
            return Task.FromResult(RefundIdToReturn);
        }

        public PaymentWebhookEvent? ParseWebhook(string body) => WebhookToReturn;
    }
}
