using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Application.Abstractions.Captcha;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Shouldly;

namespace DevStart.IntegrationTests.Auth
{
    /// <summary>
    /// Enforcement is off for every other test class (Captcha:Enabled=false), so these tests turn it on
    /// around themselves via the mutable options monitor the factory installs, and restore it on the way
    /// out. Test classes in this collection run serially, so the shared options object is safe.
    /// </summary>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class CaptchaTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private const string HeaderName = "X-Captcha-Token";

        private static object LoginBody(string email, string password) => new { email, password };

        [Fact]
        public async Task Login_WithoutHeader_Returns400_AndDoesNotCallTheProvider()
        {
            using var _ = EnableCaptcha();
            HttpClient client = CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/users/login", LoginBody("someone@devstart.test", DefaultPassword));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ProblemTitle(response)).ShouldBe("Captcha.Missing");

            // A missing token is rejected locally — spending an outbound HTTPS call on it would hand
            // an attacker a free amplification vector.
            Factory.Captcha.Calls.ShouldBeEmpty();
        }

        [Fact]
        public async Task Login_WithRejectedToken_Returns400()
        {
            using var _ = EnableCaptcha();
            Factory.Captcha.NextOutcome = CaptchaOutcome.Bot;

            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add(HeaderName, "rejected-token");

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/users/login", LoginBody("someone@devstart.test", DefaultPassword));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ProblemTitle(response)).ShouldBe("Captcha.Failed");
        }

        [Fact]
        public async Task Login_WithAcceptedToken_ReachesTheHandler_AndForwardsTheClientIp()
        {
            using var _ = EnableCaptcha();
            User user = await SeedUserAsync(verified: true, acceptMandatoryConsents: true);

            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add(HeaderName, "good-token");

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/users/login", LoginBody(user.Email, DefaultPassword));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            Factory.Captcha.Calls.Count.ShouldBe(1);
            Factory.Captcha.Calls[0].Token.ShouldBe("good-token");
            // Proves the filter reads the IP after UseForwardedHeaders has rewritten it, rather than
            // seeing the loopback address of the test host.
            Factory.Captcha.Calls[0].Ip.ShouldNotBeNullOrWhiteSpace();
            Factory.Captcha.Calls[0].Ip.ShouldNotBe("127.0.0.1");
        }

        [Fact]
        public async Task Login_MissingToken_DoesNotLeakAccountExistence()
        {
            using var _ = EnableCaptcha();
            User user = await SeedUserAsync(verified: true);
            HttpClient client = CreateClient();

            HttpResponseMessage known = await client.PostAsJsonAsync(
                "api/users/login", LoginBody(user.Email, DefaultPassword));
            HttpResponseMessage unknown = await client.PostAsJsonAsync(
                "api/users/login", LoginBody("nobody@devstart.test", DefaultPassword));

            // The filter short-circuits before the handler touches the database, so a seeded and an
            // unseeded email are indistinguishable. This is the regression test that catches a future
            // refactor moving the check behind the user lookup.
            known.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            unknown.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ProblemTitle(known)).ShouldBe(await ProblemTitle(unknown));
        }

        [Fact]
        public async Task Login_WhenProviderUnavailable_AndFailOpen_ReachesTheHandler()
        {
            using var _ = EnableCaptcha();
            Factory.CaptchaOptions.FailOpen = true;
            Factory.Captcha.NextOutcome = CaptchaOutcome.Unavailable;

            User user = await SeedUserAsync(verified: true, acceptMandatoryConsents: true);
            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add(HeaderName, "any-token");

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/users/login", LoginBody(user.Email, DefaultPassword));

            // A captcha outage must not take login down with it.
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Login_WhenProviderUnavailable_AndFailClosed_Returns503()
        {
            using var _ = EnableCaptcha();
            Factory.CaptchaOptions.FailOpen = false;
            Factory.Captcha.NextOutcome = CaptchaOutcome.Unavailable;

            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add(HeaderName, "any-token");

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/users/login", LoginBody("someone@devstart.test", DefaultPassword));

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
            (await ProblemTitle(response)).ShouldBe("Captcha.Unavailable");
        }

        [Fact]
        public async Task OAuthStart_WithoutHeader_Returns400()
        {
            // Coverage for a GET endpoint, where a captcha_token body field would have been impossible —
            // the reason the token travels in a header.
            using var _ = EnableCaptcha();
            HttpClient client = CreateClient();

            HttpResponseMessage response = await client.GetAsync("api/auth/oauth/google/start");

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ProblemTitle(response)).ShouldBe("Captcha.Missing");
        }

        [Fact]
        public async Task ResendVerification_WithHeader_IsAccepted()
        {
            // Coverage for the query-parameter-only endpoint, the other shape a body field could not serve.
            using var _ = EnableCaptcha();
            User user = await SeedUserAsync(verified: false);

            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add(HeaderName, "good-token");

            HttpResponseMessage response = await client.PostAsync(
                $"api/email-verification/resend?email={Uri.EscapeDataString(user.Email)}", content: null);

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            Factory.Captcha.Calls.ShouldContain(c => c.Token == "good-token");
        }

        [Fact]
        public async Task Login_WhenCaptchaDisabled_IgnoresTheMissingHeader()
        {
            // The kill switch, and the regression test protecting every other auth test file: with
            // Captcha:Enabled=false a bare request must behave exactly as it did before this feature.
            Factory.CaptchaOptions.Enabled = false;
            User user = await SeedUserAsync(verified: true, acceptMandatoryConsents: true);
            HttpClient client = CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/users/login", LoginBody(user.Email, DefaultPassword));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Factory.Captcha.Calls.ShouldBeEmpty();
        }

        [Fact]
        public async Task Login_InShadowMode_AllowsARejectedToken()
        {
            using var _ = EnableCaptcha();
            Factory.CaptchaOptions.ShadowMode = true;
            Factory.Captcha.NextOutcome = CaptchaOutcome.Bot;

            User user = await SeedUserAsync(verified: true, acceptMandatoryConsents: true);
            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add(HeaderName, "rejected-token");

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/users/login", LoginBody(user.Email, DefaultPassword));

            // Shadow mode measures without blocking, so the verdict is logged and ignored.
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Factory.Captcha.Calls.Count.ShouldBe(1);
        }

        [Fact]
        public async Task Login_InShadowMode_AllowsAMissingToken()
        {
            using var _ = EnableCaptcha();
            Factory.CaptchaOptions.ShadowMode = true;

            User user = await SeedUserAsync(verified: true, acceptMandatoryConsents: true);
            HttpClient client = CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/users/login", LoginBody(user.Email, DefaultPassword));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        /// <summary>
        /// Turns enforcement on for the duration of a test and puts every captcha knob back afterwards,
        /// so a failure here cannot leak into the next class in the collection.
        /// </summary>
        private IDisposable EnableCaptcha()
        {
            Factory.Captcha.Reset();
            Factory.CaptchaOptions.Enabled = true;
            Factory.CaptchaOptions.FailOpen = true;
            Factory.CaptchaOptions.ShadowMode = false;
            return new CaptchaScope(Factory);
        }

        private sealed class CaptchaScope(IntegrationTestWebAppFactory factory) : IDisposable
        {
            public void Dispose()
            {
                factory.CaptchaOptions.Enabled = false;
                factory.CaptchaOptions.FailOpen = true;
                factory.CaptchaOptions.ShadowMode = false;
                factory.Captcha.Reset();
            }
        }

        private static async Task<string> ProblemTitle(HttpResponseMessage response)
        {
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("title").GetString()!;
        }
    }
}
