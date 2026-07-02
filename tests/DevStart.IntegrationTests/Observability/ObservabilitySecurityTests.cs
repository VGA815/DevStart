using System.Net;
using System.Text.Json;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace DevStart.IntegrationTests.Observability
{
    /// <summary>
    /// Covers the security-sensitive wiring in Program.cs: the Hangfire dashboard gate (dev-open,
    /// trusted-proxy header, admin bearer) and the split health endpoints (admin-only /health/details,
    /// public terse /health/ready). The shared factory runs as "Testing", so the dashboard filter's
    /// Development bypass is off and these tests exercise the production paths.
    /// </summary>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class ObservabilitySecurityTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private const string ProxyHeader = "X-Hangfire-Auth";

        [Fact]
        public async Task Hangfire_InDevelopmentEnvironment_IsOpen()
        {
            // A derived host over the same database, differing only in environment name.
            using WebApplicationFactory<DevStart.WebApi.Program> devFactory =
                Factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

            using HttpClient client = devFactory.CreateClient(
                new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            HttpResponseMessage response = await client.GetAsync("/hangfire");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Hangfire_Anonymous_ReturnsUnauthorized()
        {
            HttpResponseMessage response = await CreateClient().GetAsync("/hangfire");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Hangfire_WithCorrectProxyHeader_IsAllowed()
        {
            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add(ProxyHeader, IntegrationTestWebAppFactory.HangfireProxySecret);

            HttpResponseMessage response = await client.GetAsync("/hangfire");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Hangfire_WithWrongProxyHeader_ReturnsUnauthorized()
        {
            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add(ProxyHeader, "not-the-configured-secret");

            HttpResponseMessage response = await client.GetAsync("/hangfire");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Hangfire_AsAdminBearer_IsAllowed()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);

            HttpResponseMessage response = await CreateAuthenticatedClient(admin).GetAsync("/hangfire");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Hangfire_AsRegularUserBearer_ReturnsForbidden()
        {
            User regular = await SeedUserAsync(role: UserSystemRole.User);

            HttpResponseMessage response = await CreateAuthenticatedClient(regular).GetAsync("/hangfire");

            // Hangfire's middleware answers 403 (not 401) when a denied request is authenticated.
            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task HealthDetails_Anonymous_ReturnsUnauthorized()
        {
            HttpResponseMessage response = await CreateClient().GetAsync("/health/details");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task HealthDetails_AsRegularUser_ReturnsForbidden()
        {
            User regular = await SeedUserAsync(role: UserSystemRole.User);

            HttpResponseMessage response = await CreateAuthenticatedClient(regular).GetAsync("/health/details");

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task HealthDetails_AsAdmin_PassesAuthorization()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);

            HttpResponseMessage response = await CreateAuthenticatedClient(admin).GetAsync("/health/details");

            // External dependencies are absent in tests, so the report itself may be 503 — the point
            // is that the admin gets past the authorization gate to the detailed body.
            response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task HealthReady_IsPubliclyAccessible_WithTerseBody()
        {
            HttpResponseMessage response = await CreateClient().GetAsync("/health/ready");

            // Anonymous access must not be rejected; the aggregate status reflects (absent) deps.
            response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

            string body = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(body);

            // The public body carries names and statuses only — no descriptions or exception text.
            doc.RootElement.EnumerateObject().Select(p => p.Name)
                .ShouldBe(["status", "checks"], ignoreOrder: true);

            foreach (JsonElement check in doc.RootElement.GetProperty("checks").EnumerateArray())
            {
                check.EnumerateObject().Select(p => p.Name)
                    .ShouldBe(["name", "status"], ignoreOrder: true);
            }
        }
    }
}
