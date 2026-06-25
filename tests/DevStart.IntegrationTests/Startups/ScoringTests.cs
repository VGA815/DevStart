using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Shouldly;

namespace DevStart.IntegrationTests.Startups
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class ScoringTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private static object CreateBody(Guid userId, string name) => new
        {
            user_id = userId,
            name,
            public_email = "founders@startup.test",
            description = "A startup building integration-tested things.",
            url = "https://startup.test",
            is_stopped = false,
            stage = 2, // Mvp — Berkus + Scorecard + VC all apply
            social_media_links = new[] { "https://t.me/startup" },
            location = 0,
            billing_email = "billing@startup.test",
            avatar_id = (Guid?)null,
            product_name = "DevWidget",
            product_problem_solution = "Solves the cold-start testing problem.",
            stack = new[] { "C#", "PostgreSQL" },
            product_value_proposition = "Ship with confidence.",
            product_differentiators = "Real database, real pipeline.",
            short_description = "Integration testing made easy.",
            industry = 1, // Saas
            target_round_amount = 50_000_000m,
            has_strategic_partnerships = true,
        };

        private async Task<Guid> CreateStartupAsync(User owner, string name)
        {
            HttpResponseMessage create = await CreateAuthenticatedClient(owner)
                .PostAsJsonAsync("api/startups/", CreateBody(owner.Id, name));
            create.StatusCode.ShouldBe(HttpStatusCode.OK);
            return await create.Content.ReadFromJsonAsync<Guid>();
        }

        [Fact]
        public async Task GetScore_AsMember_ReturnsRealValuationRangeBreakdownAndVersion()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Valuation Co");

            HttpResponseMessage response = await CreateAuthenticatedClient(owner)
                .GetAsync($"api/startups/{startupId}/score");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement root = doc.RootElement;

            decimal low = GetDecimal(root, "valuationLow");
            decimal high = GetDecimal(root, "valuationHigh");
            decimal point = GetDecimal(root, "valuationPoint");

            // No longer the 0/0 stub: a real, ordered range.
            low.ShouldBeGreaterThan(0m);
            low.ShouldBeLessThanOrEqualTo(point);
            point.ShouldBeLessThanOrEqualTo(high);

            // Methodology version + the stage-applicable methods are surfaced.
            GetProperty(root, "methodologyVersion").GetString().ShouldNotBeNullOrEmpty();

            string[] methods = GetProperty(root, "methodsUsed")
                .EnumerateArray().Select(e => e.GetString()!).ToArray();
            methods.ShouldContain("Berkus");
            methods.ShouldContain("Scorecard");
            methods.ShouldContain("VcMethod");

            // The rich breakdown is present with per-method values and weights.
            JsonElement breakdown = GetProperty(root, "valuationMethods");
            breakdown.GetArrayLength().ShouldBe(3);
        }

        [Fact]
        public async Task GetScore_AsNonMemberWithoutPro_IsForbidden()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Gated Co");

            User outsider = await SeedUserAsync();
            HttpResponseMessage response = await CreateAuthenticatedClient(outsider)
                .GetAsync($"api/startups/{startupId}/score");

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        // Property-name-policy-agnostic helpers (camelCase or snake_case both resolve).
        private static JsonElement GetProperty(JsonElement root, string camelCaseName)
        {
            foreach (JsonProperty p in root.EnumerateObject())
            {
                if (string.Equals(p.Name.Replace("_", ""), camelCaseName, StringComparison.OrdinalIgnoreCase))
                {
                    return p.Value;
                }
            }
            throw new KeyNotFoundException($"Property '{camelCaseName}' not found in response.");
        }

        private static decimal GetDecimal(JsonElement root, string camelCaseName) =>
            GetProperty(root, camelCaseName).GetDecimal();
    }
}
