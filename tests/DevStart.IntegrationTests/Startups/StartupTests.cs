using System.Net;
using System.Net.Http.Json;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Startups
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class StartupTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private static object CreateBody(Guid userId, string name) => new
        {
            user_id = userId,
            name,
            public_email = "founders@startup.test",
            description = "A startup building integration-tested things.",
            url = "https://startup.test",
            is_stopped = false,
            stage = 2, // Mvp
            social_media_links = new[] { "https://t.me/startup" },
            location = 0, // Russia
            billing_email = "billing@startup.test",
            avatar_id = (Guid?)null,
            product_name = "DevWidget",
            product_problem_solution = "Solves the cold-start testing problem.",
            stack = new[] { "C#", "PostgreSQL" },
            product_value_proposition = "Ship with confidence.",
            product_differentiators = "Real database, real pipeline.",
            short_description = "Integration testing made easy.",
        };

        [Fact]
        public async Task CreateStartup_AsOwner_PersistsStartupMemberAndProduct_AndIsReadable()
        {
            User user = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(user);

            HttpResponseMessage create = await client.PostAsJsonAsync(
                "api/startups/", CreateBody(user.Id, "Integration Co"));

            create.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid startupId = await create.Content.ReadFromJsonAsync<Guid>();
            startupId.ShouldNotBe(Guid.Empty);

            // The handler creates the startup, a founder membership and the product in one transaction.
            await ExecuteDbAsync(async db =>
            {
                (await db.Startups.AnyAsync(s => s.Id == startupId && s.Name == "Integration Co")).ShouldBeTrue();
                (await db.StartupMembers.AnyAsync(m =>
                    m.StartupId == startupId && m.ProfileId == user.Id && m.Role == StartupRole.Founder)).ShouldBeTrue();
                (await db.StartupProducts.AnyAsync(p => p.StartupId == startupId)).ShouldBeTrue();
            });

            // The public read endpoint returns it.
            HttpResponseMessage get = await client.GetAsync($"api/startups/{startupId}");
            get.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateStartup_WithoutToken_ReturnsUnauthorized()
        {
            User user = await SeedUserAsync();
            HttpClient anonymous = CreateClient();

            HttpResponseMessage response = await anonymous.PostAsJsonAsync(
                "api/startups/", CreateBody(user.Id, "Anon Co"));

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateStartup_WithDuplicateName_ReturnsConflict()
        {
            User first = await SeedUserAsync();
            User second = await SeedUserAsync();

            HttpResponseMessage created = await CreateAuthenticatedClient(first)
                .PostAsJsonAsync("api/startups/", CreateBody(first.Id, "Unique Name Co"));
            created.StatusCode.ShouldBe(HttpStatusCode.OK);

            HttpResponseMessage duplicate = await CreateAuthenticatedClient(second)
                .PostAsJsonAsync("api/startups/", CreateBody(second.Id, "Unique Name Co"));

            duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task GetStartupById_WhenMissing_ReturnsNotFound()
        {
            HttpResponseMessage response = await CreateClient().GetAsync($"api/startups/{Guid.NewGuid()}");

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
    }
}
