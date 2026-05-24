using DevStart.Application.Abstractions.Payments;
using DevStart.Infrastructure.Payments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Infrastructure.Payments;

public sealed class YooKassaWebhookParsingTests
{
    private static YooKassaPaymentProvider CreateProvider() => new(
        new HttpClient(),
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
    public void ParseWebhook_PaymentSucceeded_ReturnsPaymentId()
    {
        PaymentWebhookEvent? result = CreateProvider().ParseWebhook(
            """{"event":"payment.succeeded","object":{"id":"pay-1","status":"succeeded"}}""");

        result.ShouldNotBeNull();
        result!.Kind.ShouldBe(WebhookEventKind.PaymentSucceeded);
        result.ProviderPaymentId.ShouldBe("pay-1");
    }

    [Fact]
    public void ParseWebhook_PaymentCanceled_ReturnsPaymentId()
    {
        PaymentWebhookEvent? result = CreateProvider().ParseWebhook(
            """{"event":"payment.canceled","object":{"id":"pay-2","status":"canceled"}}""");

        result!.Kind.ShouldBe(WebhookEventKind.PaymentCanceled);
        result.ProviderPaymentId.ShouldBe("pay-2");
    }

    [Fact]
    public void ParseWebhook_RefundSucceeded_ReturnsPaymentIdFromRefund()
    {
        PaymentWebhookEvent? result = CreateProvider().ParseWebhook(
            """{"event":"refund.succeeded","object":{"id":"ref-1","status":"succeeded","payment_id":"pay-3"}}""");

        result!.Kind.ShouldBe(WebhookEventKind.RefundSucceeded);
        result.ProviderPaymentId.ShouldBe("pay-3");
    }

    [Fact]
    public void ParseWebhook_UnknownEvent_ReturnsUnsupported()
    {
        PaymentWebhookEvent? result = CreateProvider().ParseWebhook(
            """{"event":"payment.waiting_for_capture","object":{"id":"pay-4","status":"waiting_for_capture"}}""");

        result!.Kind.ShouldBe(WebhookEventKind.Unsupported);
    }

    [Fact]
    public void ParseWebhook_MalformedBody_ReturnsNull()
    {
        CreateProvider().ParseWebhook("not-json").ShouldBeNull();
    }
}
