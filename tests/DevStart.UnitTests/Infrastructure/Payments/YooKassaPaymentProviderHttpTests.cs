using DevStart.Application.Abstractions.Payments;
using DevStart.Domain.Payments;
using DevStart.Infrastructure.Payments;
using DevStart.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Net;

namespace DevStart.UnitTests.Infrastructure.Payments;

public sealed class YooKassaPaymentProviderHttpTests
{
    private static YooKassaPaymentProvider CreateProvider(HttpMessageHandler handler) => new(
        new HttpClient(handler),
        Options.Create(new YooKassaOptions
        {
            ApiUrl = "https://api.yookassa.ru",
            ShopId = "shop",
            SecretKey = "secret",
            ReturnUrl = "https://example.com/return",
        }),
        Options.Create(new YooKassaReceiptOptions()),
        NullLogger<YooKassaPaymentProvider>.Instance);

    [Fact]
    public async Task CreatePaymentAsync_SendsReceiptAndMetadata_AndReturnsConfirmation()
    {
        var handler = new CapturingHttpMessageHandler(
            HttpStatusCode.OK,
            """{"id":"pay-1","status":"pending","confirmation":{"type":"redirect","confirmation_url":"https://pay/redirect"}}""");
        YooKassaPaymentProvider provider = CreateProvider(handler);

        Guid paymentId = Guid.NewGuid();
        var input = new CreatePaymentInput(
            990m, "RUB", "DevStart Pro — 30 days", "https://example.com/return",
            "idem-key-1", "buyer@example.com", paymentId, Guid.NewGuid(), Guid.NewGuid());

        CreatedPayment created = await provider.CreatePaymentAsync(input, CancellationToken.None);

        created.ProviderPaymentId.ShouldBe("pay-1");
        created.ConfirmationUrl.ShouldBe("https://pay/redirect");

        string body = handler.LastRequestBody.ShouldNotBeNull();
        body.ShouldContain("\"receipt\"");
        body.ShouldContain("buyer@example.com");
        body.ShouldContain("\"vat_code\":1");
        body.ShouldContain("\"payment_subject\":\"service\"");
        body.ShouldContain("\"payment_mode\":\"full_payment\"");
        body.ShouldContain("\"metadata\"");
        body.ShouldContain(paymentId.ToString());
        body.ShouldContain("\"capture\":true");
        handler.LastRequest!.Headers.Contains("Idempotence-Key").ShouldBeTrue();
    }

    [Fact]
    public async Task GetPaymentAsync_ParsesStatusPaidAndRefundedAmount()
    {
        var handler = new CapturingHttpMessageHandler(
            HttpStatusCode.OK,
            """{"id":"pay-1","status":"succeeded","paid":true,"captured_at":"2026-05-20T10:00:00Z","refunded_amount":{"value":"990.00","currency":"RUB"},"receipt_registration":"succeeded"}""");
        YooKassaPaymentProvider provider = CreateProvider(handler);

        ProviderPaymentSnapshot? snapshot = await provider.GetPaymentAsync("pay-1", CancellationToken.None);

        snapshot.ShouldNotBeNull();
        snapshot!.Status.ShouldBe(PaymentStatus.Succeeded);
        snapshot.Paid.ShouldBeTrue();
        snapshot.RefundedAmount.ShouldBe(990m);
        snapshot.ReceiptRegistration.ShouldBe("succeeded");
    }

    [Fact]
    public async Task GetPaymentAsync_OnError_ReturnsNull()
    {
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.NotFound, "{}");
        YooKassaPaymentProvider provider = CreateProvider(handler);

        ProviderPaymentSnapshot? snapshot = await provider.GetPaymentAsync("missing", CancellationToken.None);

        snapshot.ShouldBeNull();
    }

    [Fact]
    public async Task CreateRefundAsync_SendsPaymentIdAndReceipt_AndReturnsRefundId()
    {
        var handler = new CapturingHttpMessageHandler(
            HttpStatusCode.OK,
            """{"id":"ref-1","status":"succeeded"}""");
        YooKassaPaymentProvider provider = CreateProvider(handler);

        var input = new CreateRefundInput(
            "pay-1", 990m, "RUB", "Возврат — DevStart Pro", "buyer@example.com", "refund:pay-1:990.00");

        string refundId = await provider.CreateRefundAsync(input, CancellationToken.None);

        refundId.ShouldBe("ref-1");
        string body = handler.LastRequestBody.ShouldNotBeNull();
        body.ShouldContain("\"payment_id\":\"pay-1\"");
        body.ShouldContain("\"receipt\"");
        body.ShouldContain("buyer@example.com");
    }
}
