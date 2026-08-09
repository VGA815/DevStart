using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Auth
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class SessionsTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private const string Chrome =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
        private const string Firefox =
            "Mozilla/5.0 (X11; Linux x86_64; rv:127.0) Gecko/20100101 Firefox/127.0";

        private static async Task<JsonElement> ReadRootAsync(HttpResponseMessage response)
        {
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.Clone();
        }

        /// <summary>Logs in with a given User-Agent and returns the token pair.</summary>
        private async Task<(string AccessToken, string RefreshToken)> LoginAsync(User user, string userAgent)
        {
            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

            HttpResponseMessage login = await client.PostAsJsonAsync(
                "api/users/login", new { email = user.Email, password = DefaultPassword });
            login.StatusCode.ShouldBe(HttpStatusCode.OK);

            JsonElement tokens = (await ReadRootAsync(login)).GetProperty("tokens");
            return (tokens.GetProperty("accessToken").GetString()!, tokens.GetProperty("refreshToken").GetString()!);
        }

        private HttpClient ClientWithToken(string accessToken)
        {
            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
            return client;
        }

        [Fact]
        public async Task Sessions_ListOnePerLogin_AndMarkTheCallersOwn()
        {
            User user = await SeedUserAsync();
            (string firstAccess, _) = await LoginAsync(user, Chrome);
            await LoginAsync(user, Firefox);

            HttpResponseMessage response = await ClientWithToken(firstAccess).GetAsync("api/users/me/sessions");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            JsonElement sessions = await ReadRootAsync(response);
            sessions.GetArrayLength().ShouldBe(2);

            JsonElement[] items = [.. sessions.EnumerateArray()];
            items.Count(s => s.GetProperty("current").GetBoolean()).ShouldBe(1);
            items.Select(s => s.GetProperty("browser").GetString()).ShouldBe(
                ["Chrome", "Firefox"], ignoreOrder: true);
        }

        [Fact]
        public async Task Session_SurvivesRefresh_UnderTheSameId()
        {
            User user = await SeedUserAsync();
            (string access, string refresh) = await LoginAsync(user, Chrome);

            Guid before = (await ReadRootAsync(await ClientWithToken(access).GetAsync("api/users/me/sessions")))[0]
                .GetProperty("id").GetGuid();

            HttpResponseMessage refreshed = await CreateClient().PostAsJsonAsync(
                "api/auth/refresh", new { refresh_token = refresh });
            refreshed.StatusCode.ShouldBe(HttpStatusCode.OK);
            string newAccess = (await ReadRootAsync(refreshed)).GetProperty("accessToken").GetString()!;

            JsonElement after = await ReadRootAsync(await ClientWithToken(newAccess).GetAsync("api/users/me/sessions"));

            // Rotation creates a new row; the session id must not move, or the list and the revoke
            // endpoint would go stale on every refresh.
            after.GetArrayLength().ShouldBe(1);
            after[0].GetProperty("id").GetGuid().ShouldBe(before);
            after[0].GetProperty("current").GetBoolean().ShouldBeTrue();
        }

        [Fact]
        public async Task RevokingASession_StopsItsRefreshToken()
        {
            User user = await SeedUserAsync();
            (string keepAccess, _) = await LoginAsync(user, Chrome);
            (string victimAccess, string victimRefresh) = await LoginAsync(user, Firefox);

            Guid victimSessionId = (await ReadRootAsync(
                await ClientWithToken(victimAccess).GetAsync("api/users/me/sessions")))
                .EnumerateArray().Single(s => s.GetProperty("current").GetBoolean())
                .GetProperty("id").GetGuid();

            HttpResponseMessage revoke = await ClientWithToken(keepAccess)
                .DeleteAsync($"api/users/me/sessions/{victimSessionId}");
            revoke.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            HttpResponseMessage refreshed = await CreateClient().PostAsJsonAsync(
                "api/auth/refresh", new { refresh_token = victimRefresh });
            refreshed.StatusCode.ShouldNotBe(HttpStatusCode.OK);

            JsonElement remaining = await ReadRootAsync(
                await ClientWithToken(keepAccess).GetAsync("api/users/me/sessions"));
            remaining.GetArrayLength().ShouldBe(1);
        }

        [Fact]
        public async Task RevokeAll_KeepsTheCallersOwnSessionByDefault()
        {
            User user = await SeedUserAsync();
            (string keepAccess, string keepRefresh) = await LoginAsync(user, Chrome);
            (_, string otherRefresh) = await LoginAsync(user, Firefox);

            HttpResponseMessage revokeAll = await ClientWithToken(keepAccess)
                .PostAsJsonAsync("api/users/me/sessions/revoke-all", new { include_current = false });
            revokeAll.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            // Signing the user out of the tab they pressed the button in would read as a bug.
            HttpResponseMessage mine = await CreateClient().PostAsJsonAsync(
                "api/auth/refresh", new { refresh_token = keepRefresh });
            mine.StatusCode.ShouldBe(HttpStatusCode.OK);

            HttpResponseMessage theirs = await CreateClient().PostAsJsonAsync(
                "api/auth/refresh", new { refresh_token = otherRefresh });
            theirs.StatusCode.ShouldNotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task RevokeAll_WithIncludeCurrent_EndsEverySession()
        {
            User user = await SeedUserAsync();
            (string access, string refresh) = await LoginAsync(user, Chrome);

            HttpResponseMessage revokeAll = await ClientWithToken(access)
                .PostAsJsonAsync("api/users/me/sessions/revoke-all", new { include_current = true });
            revokeAll.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            HttpResponseMessage refreshed = await CreateClient().PostAsJsonAsync(
                "api/auth/refresh", new { refresh_token = refresh });
            refreshed.StatusCode.ShouldNotBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task AnotherUsersSessionId_Returns404()
        {
            User owner = await SeedUserAsync();
            User other = await SeedUserAsync();
            (string ownerAccess, _) = await LoginAsync(owner, Chrome);
            (string otherAccess, _) = await LoginAsync(other, Chrome);

            Guid ownerSessionId = (await ReadRootAsync(
                await ClientWithToken(ownerAccess).GetAsync("api/users/me/sessions")))[0]
                .GetProperty("id").GetGuid();

            HttpResponseMessage response = await ClientWithToken(otherAccess)
                .DeleteAsync($"api/users/me/sessions/{ownerSessionId}");

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await ExecuteDbAsync(db => db.RefreshTokens
                .CountAsync(t => t.UserId == owner.Id && t.RevokedAt == null))).ShouldBe(1);
        }

        [Fact]
        public async Task Sessions_RequireAuthentication()
        {
            HttpResponseMessage response = await CreateClient().GetAsync("api/users/me/sessions");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}
