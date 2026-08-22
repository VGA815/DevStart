using System.Net;
using System.Net.Http.Json;
using DevStart.Domain.StartupCompetitors;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Startups
{
    /// <summary>
    /// Competitor-card hygiene (SC-38): a card must carry a real website, the same domain cannot be
    /// listed twice within one startup, and the list has an upper bound. Without these the
    /// quality-of-analysis half of the competition score would be farmable with placeholder cards.
    /// </summary>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class StartupCompetitorTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private const string Endpoint = "api/startup-competitors";

        private static object CreateStartupBody(Guid userId, string name) => new
        {
            user_id = userId,
            name,
            public_email = "founders@startup.test",
            description = "A startup building integration-tested things.",
            url = "https://startup.test",
            is_stopped = false,
            stage = 2, // Mvp
            social_media_links = new[] { "https://t.me/startup" },
            location = 0,
            billing_email = "billing@startup.test",
            avatar_id = (Guid?)null,
            product_problem = "Cold-start testing is unreliable.",
            product_solution = "Solves the cold-start testing problem.",
            stack = new[] { "C#" },
            product_value_proposition = "Ship with confidence.",
            product_differentiators = "Real database, real pipeline.",
            short_description = "Integration testing made easy.",
            industry = 1, // Saas
            target_round_amount = 50_000_000m,
        };

        private static object CompetitorBody(
            Guid startupId, string name, string? website, string? strengths = null) => new
            {
                startup_id = startupId,
                name,
                website,
                description = (string?)null,
                strengths_vs_us = strengths,
                weaknesses_vs_us = (string?)null,
            };

        private async Task<(HttpClient Client, Guid StartupId)> CreateStartupAsync()
        {
            User owner = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(owner);
            HttpResponseMessage create = await client.PostAsJsonAsync(
                "api/startups/", CreateStartupBody(owner.Id, "Competitor Hygiene Co"));
            create.StatusCode.ShouldBe(HttpStatusCode.OK);
            return (client, await create.Content.ReadFromJsonAsync<Guid>());
        }

        [Fact]
        public async Task Create_WithoutWebsite_ReturnsBadRequest()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                Endpoint, CompetitorBody(startupId, "Rival", website: null));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ExecuteDbAsync(db => db.StartupCompetitors.AnyAsync())).ShouldBeFalse();
        }

        [Fact]
        public async Task Create_WithNonAbsoluteWebsite_ReturnsBadRequest()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                Endpoint, CompetitorBody(startupId, "Rival", "rival.com"));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_StoresTheNormalizedDomain()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                Endpoint, CompetitorBody(startupId, "Rival", "https://WWW.Rival.com/pricing"));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid id = await response.Content.ReadFromJsonAsync<Guid>();

            StartupCompetitor row = await ExecuteDbAsync(db => db.StartupCompetitors.SingleAsync(c => c.Id == id));
            row.NormalizedDomain.ShouldBe("rival.com");
        }

        [Fact]
        public async Task Create_WithADomainAlreadyListed_ReturnsConflict()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            (await client.PostAsJsonAsync(Endpoint, CompetitorBody(startupId, "Rival", "https://rival.com")))
                .EnsureSuccessStatusCode();

            // Same domain behind a different URL — still the same competitor.
            HttpResponseMessage duplicate = await client.PostAsJsonAsync(
                Endpoint, CompetitorBody(startupId, "Rival (again)", "https://www.rival.com/about"));

            duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await duplicate.Content.ReadAsStringAsync()).ShouldContain("StartupCompetitors.DuplicateDomain");
            (await ExecuteDbAsync(db => db.StartupCompetitors.CountAsync())).ShouldBe(1);
        }

        [Fact]
        public async Task Update_ToADomainAlreadyListed_ReturnsConflict_ButKeepingItsOwnDomainIsFine()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            (await client.PostAsJsonAsync(Endpoint, CompetitorBody(startupId, "Rival A", "https://rival-a.com")))
                .EnsureSuccessStatusCode();
            HttpResponseMessage createB = await client.PostAsJsonAsync(
                Endpoint, CompetitorBody(startupId, "Rival B", "https://rival-b.com"));
            Guid bId = await createB.Content.ReadFromJsonAsync<Guid>();

            HttpResponseMessage collide = await client.PutAsJsonAsync($"{Endpoint}/{bId}", new
            {
                name = "Rival B",
                website = "https://rival-a.com",
                description = (string?)null,
                strengths_vs_us = (string?)null,
                weaknesses_vs_us = (string?)null,
            });
            collide.StatusCode.ShouldBe(HttpStatusCode.Conflict);

            // Re-saving a card on its own domain must not trip the dedup check against itself.
            HttpResponseMessage ownDomain = await client.PutAsJsonAsync($"{Endpoint}/{bId}", new
            {
                name = "Rival B renamed",
                website = "https://rival-b.com",
                description = (string?)null,
                strengths_vs_us = "Bigger sales team",
                weaknesses_vs_us = (string?)null,
            });
            ownDomain.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Create_BeyondTheLimit_IsRejected()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            // Fill the list to the limit directly — going through the API 50 times would only test HTTP.
            await ExecuteDbAsync(async db =>
            {
                for (int i = 0; i < StartupCompetitor.MaxPerStartup; i++)
                {
                    db.StartupCompetitors.Add(StartupCompetitor.Create(
                        startupId, $"Rival {i}", $"https://rival{i}.com", null, null, null, DateTime.UtcNow));
                }
                await db.SaveChangesAsync();
            });

            HttpResponseMessage response = await client.PostAsJsonAsync(
                Endpoint, CompetitorBody(startupId, "One too many", "https://one-too-many.com"));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).ShouldContain("StartupCompetitors.LimitReached");
            (await ExecuteDbAsync(db => db.StartupCompetitors.CountAsync()))
                .ShouldBe(StartupCompetitor.MaxPerStartup);
        }
    }
}
