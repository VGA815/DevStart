using System.Net;
using System.Net.Http.Json;
using DevStart.IntegrationTests.Infrastructure;
using Shouldly;

namespace DevStart.IntegrationTests.Auth
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class AuthRateLimitingTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        [Fact]
        public async Task Login_BeyondThePerIpLimit_Returns429()
        {
            // The "auth" policy is a fixed window of 10 requests/minute per client IP. All requests from a
            // single client share one IP (and thus one partition), so the 11th request is rejected.
            HttpClient client = CreateClient();

            var statuses = new List<HttpStatusCode>();
            for (int i = 0; i < 11; i++)
            {
                HttpResponseMessage response = await client.PostAsJsonAsync(
                    "api/users/login", new { email = $"rl{i}@devstart.test", password = "Password123!" });
                statuses.Add(response.StatusCode);
            }

            statuses.Take(10).ShouldNotContain(HttpStatusCode.TooManyRequests);
            statuses[^1].ShouldBe(HttpStatusCode.TooManyRequests);
        }
    }
}
