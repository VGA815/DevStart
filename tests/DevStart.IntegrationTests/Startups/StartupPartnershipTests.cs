using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Domain.StartupPartnerships;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Startups
{
    /// <summary>
    /// М3: strategic partnerships are a list of records, not a checkbox. The hygiene is the
    /// competitor-card hygiene — a real website, one record per partner domain, an upper bound —
    /// because the fifth Berkus factor is now earned by worked-out records and would otherwise be
    /// farmable with placeholders under three URLs.
    /// </summary>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class StartupPartnershipTests(IntegrationTestWebAppFactory factory)
        : IntegrationTestBase(factory)
    {
        private const string Endpoint = "api/startup-partnerships";

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

        private static object PartnershipBody(
            Guid startupId, string partnerName, string? website, string? description = null) => new
            {
                startup_id = startupId,
                partner_name = partnerName,
                website,
                kind = (int)PartnershipKind.Distribution,
                description,
            };

        private async Task<(HttpClient Client, Guid StartupId)> CreateStartupAsync()
        {
            User owner = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(owner);
            HttpResponseMessage create = await client.PostAsJsonAsync(
                "api/startups/", CreateStartupBody(owner.Id, "Partnership Co"));
            create.StatusCode.ShouldBe(HttpStatusCode.OK);
            return (client, await create.Content.ReadFromJsonAsync<Guid>());
        }

        [Fact]
        public async Task Create_WithoutWebsite_ReturnsBadRequest()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                Endpoint, PartnershipBody(startupId, "Big Retailer", website: null));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ExecuteDbAsync(db => db.StartupPartnerships.AnyAsync())).ShouldBeFalse();
        }

        [Fact]
        public async Task Create_StoresTheNormalizedDomain()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                Endpoint, PartnershipBody(startupId, "Big Retailer", "https://WWW.Retailer.com/about"));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid id = await response.Content.ReadFromJsonAsync<Guid>();

            StartupPartnership row = await ExecuteDbAsync(
                db => db.StartupPartnerships.SingleAsync(p => p.Id == id));
            row.NormalizedDomain.ShouldBe("retailer.com");
        }

        [Fact]
        public async Task Create_WithADomainAlreadyListed_ReturnsConflict()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            (await client.PostAsJsonAsync(Endpoint, PartnershipBody(startupId, "Retailer", "https://retailer.com")))
                .EnsureSuccessStatusCode();

            // The same partner under a second URL — this is what listing one partnership three times
            // to take the whole Berkus ceiling would look like.
            HttpResponseMessage duplicate = await client.PostAsJsonAsync(
                Endpoint, PartnershipBody(startupId, "Retailer (pilot)", "https://www.retailer.com/pilot"));

            duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await duplicate.Content.ReadAsStringAsync()).ShouldContain("StartupPartnerships.DuplicateDomain");
            (await ExecuteDbAsync(db => db.StartupPartnerships.CountAsync())).ShouldBe(1);
        }

        [Fact]
        public async Task Update_ToADomainAlreadyListed_ReturnsConflict_ButKeepingItsOwnDomainIsFine()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            (await client.PostAsJsonAsync(Endpoint, PartnershipBody(startupId, "Partner A", "https://partner-a.com")))
                .EnsureSuccessStatusCode();
            HttpResponseMessage createB = await client.PostAsJsonAsync(
                Endpoint, PartnershipBody(startupId, "Partner B", "https://partner-b.com"));
            Guid bId = await createB.Content.ReadFromJsonAsync<Guid>();

            HttpResponseMessage collide = await client.PutAsJsonAsync($"{Endpoint}/{bId}", new
            {
                partner_name = "Partner B",
                website = "https://partner-a.com",
                kind = (int)PartnershipKind.Pilot,
                description = (string?)null,
            });
            collide.StatusCode.ShouldBe(HttpStatusCode.Conflict);

            HttpResponseMessage ownDomain = await client.PutAsJsonAsync($"{Endpoint}/{bId}", new
            {
                partner_name = "Partner B renamed",
                website = "https://partner-b.com",
                kind = (int)PartnershipKind.Pilot,
                description = "Гоняют наш продукт на своей линии с марта.",
            });
            ownDomain.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        /// <summary>
        /// The list ships whole — placeholders included — with the worked-out flag on each record.
        /// Hiding the empty ones would make "add ten records, show the two that read well" a tactic,
        /// and would leave the graded Berkus factor unexplainable from the profile.
        /// </summary>
        [Fact]
        public async Task Get_ReturnsEveryRecord_AndMarksWhichOnesAreWorkedOut()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            (await client.PostAsJsonAsync(Endpoint, PartnershipBody(
                startupId, "Retailer", "https://retailer.com", "Продают наш продукт в 40 точках с мая.")))
                .EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync(Endpoint, PartnershipBody(
                startupId, "Placeholder", "https://placeholder.com")))
                .EnsureSuccessStatusCode();

            HttpResponseMessage response = await client.GetAsync($"api/startups/{startupId}/partnerships");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            JsonElement[] records = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                .RootElement.EnumerateArray().ToArray();

            records.Length.ShouldBe(2);
            records.Count(r => r.GetProperty("isWorkedOut").GetBoolean()).ShouldBe(1);
            records.Single(r => r.GetProperty("isWorkedOut").GetBoolean())
                .GetProperty("partnerName").GetString().ShouldBe("Retailer");
        }

        [Fact]
        public async Task Create_BeyondTheLimit_IsRejected()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            // Fill the list to the limit directly — going through the API 30 times would only test HTTP.
            await ExecuteDbAsync(async db =>
            {
                for (int i = 0; i < StartupPartnership.MaxPerStartup; i++)
                {
                    db.StartupPartnerships.Add(StartupPartnership.Create(
                        startupId, $"Partner {i}", $"https://partner{i}.com", $"partner{i}.com",
                        PartnershipKind.Pilot, null, DateTime.UtcNow));
                }
                await db.SaveChangesAsync();
            });

            HttpResponseMessage response = await client.PostAsJsonAsync(
                Endpoint, PartnershipBody(startupId, "One too many", "https://one-too-many.com"));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).ShouldContain("StartupPartnerships.LimitReached");
            (await ExecuteDbAsync(db => db.StartupPartnerships.CountAsync()))
                .ShouldBe(StartupPartnership.MaxPerStartup);
        }
    }
}
