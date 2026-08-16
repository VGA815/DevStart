using DevStart.Application.Abstractions.Captcha;
using DevStart.Application.Configuration;
using DevStart.Infrastructure.Captcha;
using DevStart.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Net;

namespace DevStart.UnitTests.Infrastructure.Captcha;

public sealed class YandexSmartCaptchaVerifierTests
{
    private static YandexSmartCaptchaVerifier CreateVerifier(HttpMessageHandler handler) => new(
        new HttpClient(handler),
        Options.Create(new CaptchaOptions
        {
            Enabled = true,
            ServerKey = "server-secret",
            ValidateUrl = "https://smartcaptcha.yandexcloud.net/validate",
        }),
        NullLogger<YandexSmartCaptchaVerifier>.Instance);

    private static CapturingHttpMessageHandler Responds(HttpStatusCode status, string json) => new(status, json);

    [Fact]
    public async Task VerifyAsync_PostsSecretTokenAndIp_AsFormUrlEncoded()
    {
        CapturingHttpMessageHandler handler = Responds(HttpStatusCode.OK, """{"status":"ok"}""");
        YandexSmartCaptchaVerifier verifier = CreateVerifier(handler);

        await verifier.VerifyAsync("tok-1", "203.0.113.7", CancellationToken.None);

        string body = handler.LastRequestBody.ShouldNotBeNull();
        body.ShouldContain("secret=server-secret");
        body.ShouldContain("token=tok-1");
        body.ShouldContain("ip=203.0.113.7");
        handler.LastRequest!.Content!.Headers.ContentType!.MediaType
            .ShouldBe("application/x-www-form-urlencoded");
    }

    [Fact]
    public async Task VerifyAsync_OmitsIp_WhenNotAvailable()
    {
        CapturingHttpMessageHandler handler = Responds(HttpStatusCode.OK, """{"status":"ok"}""");
        YandexSmartCaptchaVerifier verifier = CreateVerifier(handler);

        await verifier.VerifyAsync("tok-1", clientIp: null, CancellationToken.None);

        handler.LastRequestBody.ShouldNotBeNull().ShouldNotContain("ip=");
    }

    [Fact]
    public async Task VerifyAsync_StatusOk_ReturnsHuman()
    {
        YandexSmartCaptchaVerifier verifier = CreateVerifier(Responds(HttpStatusCode.OK, """{"status":"ok"}"""));

        CaptchaOutcome outcome = await verifier.VerifyAsync("tok", null, CancellationToken.None);

        outcome.ShouldBe(CaptchaOutcome.Human);
    }

    [Fact]
    public async Task VerifyAsync_StatusFailed_ReturnsBot()
    {
        YandexSmartCaptchaVerifier verifier = CreateVerifier(
            Responds(HttpStatusCode.OK, """{"status":"failed","message":"invalid token"}"""));

        CaptchaOutcome outcome = await verifier.VerifyAsync("tok", null, CancellationToken.None);

        outcome.ShouldBe(CaptchaOutcome.Bot);
    }

    [Fact]
    public async Task VerifyAsync_ServerError_ReturnsUnavailable()
    {
        // A 5xx from the vendor must degrade, not surface as a 500 to the user trying to log in.
        YandexSmartCaptchaVerifier verifier = CreateVerifier(
            Responds(HttpStatusCode.InternalServerError, """{"status":"failed"}"""));

        CaptchaOutcome outcome = await verifier.VerifyAsync("tok", null, CancellationToken.None);

        outcome.ShouldBe(CaptchaOutcome.Unavailable);
    }

    [Fact]
    public async Task VerifyAsync_BodyWithoutStatus_ReturnsUnavailable()
    {
        YandexSmartCaptchaVerifier verifier = CreateVerifier(Responds(HttpStatusCode.OK, """{"message":"hi"}"""));

        CaptchaOutcome outcome = await verifier.VerifyAsync("tok", null, CancellationToken.None);

        outcome.ShouldBe(CaptchaOutcome.Unavailable);
    }

    [Fact]
    public async Task VerifyAsync_HttpRequestException_ReturnsUnavailable()
    {
        var handler = new CapturingHttpMessageHandler(
            _ => throw new HttpRequestException("dns failure"));
        YandexSmartCaptchaVerifier verifier = CreateVerifier(handler);

        CaptchaOutcome outcome = await verifier.VerifyAsync("tok", null, CancellationToken.None);

        outcome.ShouldBe(CaptchaOutcome.Unavailable);
    }

    [Fact]
    public async Task VerifyAsync_Timeout_ReturnsUnavailable()
    {
        // HttpClient surfaces its own timeout as TaskCanceledException with no cancellation requested.
        var handler = new CapturingHttpMessageHandler(
            _ => throw new TaskCanceledException("timed out"));
        YandexSmartCaptchaVerifier verifier = CreateVerifier(handler);

        CaptchaOutcome outcome = await verifier.VerifyAsync("tok", null, CancellationToken.None);

        outcome.ShouldBe(CaptchaOutcome.Unavailable);
    }

    [Fact]
    public async Task VerifyAsync_WhenTheCallerCancels_DoesNotMasqueradeAsUnavailable()
    {
        // A client disconnect cancels our token too. Reporting that as a captcha outage would silently
        // fail-open on a request that was never actually checked.
        var handler = new CapturingHttpMessageHandler(
            _ => throw new TaskCanceledException("caller went away"));
        YandexSmartCaptchaVerifier verifier = CreateVerifier(handler);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<TaskCanceledException>(
            async () => await verifier.VerifyAsync("tok", null, cts.Token));
    }
}
