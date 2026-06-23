using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Shouldly;

namespace DevStart.IntegrationTests.Auth
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class LoginTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private static object LoginBody(string email, string password) => new { email, password };

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsTokenPair()
        {
            User user = await SeedUserAsync(verified: true, acceptMandatoryConsents: true);
            HttpClient client = CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/users/login", LoginBody(user.Email, DefaultPassword));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement tokens = doc.RootElement.GetProperty("tokens");
            tokens.ValueKind.ShouldBe(JsonValueKind.Object);
            tokens.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
            tokens.GetProperty("refreshToken").GetString().ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Login_WithWrongPassword_And_UnknownEmail_ReturnIdenticalProblem()
        {
            User user = await SeedUserAsync(verified: true);
            HttpClient client = CreateClient();

            HttpResponseMessage wrongPassword = await client.PostAsJsonAsync(
                "api/users/login", LoginBody(user.Email, "WrongPassword!"));
            HttpResponseMessage unknownEmail = await client.PostAsJsonAsync(
                "api/users/login", LoginBody("nobody@devstart.test", DefaultPassword));

            // Enumeration-safe: a wrong password and an unknown account are indistinguishable.
            wrongPassword.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            unknownEmail.StatusCode.ShouldBe(HttpStatusCode.NotFound);

            string wrongTitle = await ProblemTitle(wrongPassword);
            string unknownTitle = await ProblemTitle(unknownEmail);
            wrongTitle.ShouldBe(unknownTitle);
        }

        [Fact]
        public async Task Login_WithUnverifiedEmail_ReturnsForbidden()
        {
            User user = await SeedUserAsync(verified: false);
            HttpClient client = CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/users/login", LoginBody(user.Email, DefaultPassword));

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await ProblemTitle(response)).ShouldBe("Users.EmailNotVerified");
        }

        private static async Task<string> ProblemTitle(HttpResponseMessage response)
        {
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("title").GetString()!;
        }
    }
}
