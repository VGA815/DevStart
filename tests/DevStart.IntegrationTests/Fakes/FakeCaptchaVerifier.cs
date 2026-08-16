using DevStart.Application.Abstractions.Captcha;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>
    /// Stands in for the Yandex SmartCaptcha HTTP call. Defaults to <see cref="CaptchaOutcome.Human"/>
    /// so that any test which happens to run with the captcha enabled still passes unless it opts into
    /// a rejection.
    /// </summary>
    internal sealed class FakeCaptchaVerifier : ICaptchaVerifier
    {
        public CaptchaOutcome NextOutcome { get; set; } = CaptchaOutcome.Human;

        /// <summary>Every verification attempt, so a test can assert we did (or did not) call out.</summary>
        public List<(string Token, string? Ip)> Calls { get; } = [];

        public Task<CaptchaOutcome> VerifyAsync(string token, string? clientIp, CancellationToken cancellationToken)
        {
            Calls.Add((token, clientIp));
            return Task.FromResult(NextOutcome);
        }

        public void Reset()
        {
            NextOutcome = CaptchaOutcome.Human;
            Calls.Clear();
        }
    }
}
