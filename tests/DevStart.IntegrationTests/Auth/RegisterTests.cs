using System.Net;
using System.Net.Http.Json;
using DevStart.Infrastructure.Database;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Auth
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class RegisterTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        // All five consent types at their seeded active version, mandatory ones accepted.
        private static object[] AllConsentsAccepted() =>
        [
            new { type = 0, document_version = "1.0", accepted = true }, // PersonalDataProcessing (mandatory)
            new { type = 1, document_version = "1.0", accepted = true }, // PrivacyPolicy (mandatory)
            new { type = 2, document_version = "1.0", accepted = true }, // TermsOfService (mandatory)
            new { type = 3, document_version = "1.0", accepted = true }, // Cookies
            new { type = 4, document_version = "1.0", accepted = true }, // PublicOffer (mandatory)
        ];

        private static object RegisterBody(string email, string username) => new
        {
            email,
            username,
            password = "Password123!",
            bio = (string?)null,
            name = "Test User",
            url = (string?)null,
            social_media_links = Array.Empty<string>(),
            is_public = false,
            consents = AllConsentsAccepted(),
        };

        [Fact]
        public async Task Register_WithValidPayload_ReturnsOk_AndPersistsUser()
        {
            HttpClient client = CreateClient();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/users/register", RegisterBody("new.user@devstart.test", "new_user"));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            Guid userId = await response.Content.ReadFromJsonAsync<Guid>();
            userId.ShouldNotBe(Guid.Empty);

            bool exists = await ExecuteDbAsync(db =>
                db.Users.AnyAsync(u => u.Id == userId && u.Email == "new.user@devstart.test"));
            exists.ShouldBeTrue();

            // The mandatory consents that were submitted should have been recorded.
            int consentCount = await ExecuteDbAsync(db => db.UserConsents.CountAsync(c => c.UserId == userId));
            consentCount.ShouldBe(5);
        }

        [Fact]
        public async Task Register_RaisesVerificationEmail()
        {
            HttpClient client = CreateClient();

            await client.PostAsJsonAsync("api/users/register", RegisterBody("verify.me@devstart.test", "verify_me"));

            Factory.EmailSender.Verifications.ShouldContain(v => v.Email == "verify.me@devstart.test");
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ReturnsConflict()
        {
            HttpClient client = CreateClient();
            object body = RegisterBody("dupe@devstart.test", "first_user");

            (await client.PostAsJsonAsync("api/users/register", body)).StatusCode.ShouldBe(HttpStatusCode.OK);

            HttpResponseMessage second = await client.PostAsJsonAsync(
                "api/users/register", RegisterBody("dupe@devstart.test", "second_user"));

            second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Register_WithoutAllConsents_ReturnsValidationProblem()
        {
            HttpClient client = CreateClient();

            // Only one consent provided — the validator requires all five types.
            object body = new
            {
                email = "missing.consents@devstart.test",
                username = "missing_consents",
                password = "Password123!",
                is_public = false,
                consents = new[] { new { type = 0, document_version = "1.0", accepted = true } },
            };

            HttpResponseMessage response = await client.PostAsJsonAsync("api/users/register", body);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

            bool exists = await ExecuteDbAsync(db =>
                db.Users.AnyAsync(u => u.Email == "missing.consents@devstart.test"));
            exists.ShouldBeFalse();
        }
    }
}
