using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Shouldly;

namespace DevStart.IntegrationTests.Auth
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class RefreshTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        [Fact]
        public async Task Refresh_WithValidToken_IssuesANewTokenPair()
        {
            User user = await SeedUserAsync(verified: true);
            HttpClient client = CreateClient();

            // Log in to obtain a genuine refresh token.
            HttpResponseMessage login = await client.PostAsJsonAsync(
                "api/users/login", new { email = user.Email, password = DefaultPassword });
            login.StatusCode.ShouldBe(HttpStatusCode.OK);

            string refreshToken;
            using (JsonDocument loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync()))
            {
                refreshToken = loginDoc.RootElement.GetProperty("tokens").GetProperty("refreshToken").GetString()!;
            }

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/auth/refresh", new { refresh_token = refreshToken });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
            string rotated = doc.RootElement.GetProperty("refreshToken").GetString()!;
            rotated.ShouldNotBeNullOrWhiteSpace();
            // The refresh token is rotated on use.
            rotated.ShouldNotBe(refreshToken);
        }

        [Fact]
        public async Task Refresh_WithInvalidToken_ReturnsProblem()
        {
            HttpClient client = CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/auth/refresh", new { refresh_token = "not-a-real-refresh-token" });

            response.IsSuccessStatusCode.ShouldBeFalse();
        }
    }
}
