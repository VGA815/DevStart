using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Application.Scoring;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.Domain.Valuation;
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

            // The version string names the *code* that produced these numbers, so configuration must
            // never disagree with the code default: a drifted config silently mislabels every snapshot
            // and term sheet it stamps, and mislabelled rows are indistinguishable from genuine ones.
            // This host boots the real appsettings.json, so the assertion catches exactly that drift —
            // it is why the v5 release shipping with a v4-pinned config went unnoticed. A legitimate
            // version bump needs no change here, only the two places that must move together.
            GetProperty(root, "methodologyVersion").GetString()
                .ShouldBe(new ValuationOptions().MethodologyVersion);

            // The stage-applicable methods are surfaced.

            string[] methods = GetProperty(root, "methodsUsed")
                .EnumerateArray().Select(e => e.GetString()!).ToArray();
            methods.ShouldContain("Berkus");
            methods.ShouldContain("Scorecard");
            methods.ShouldContain("VcMethod");

            // The rich breakdown is present with per-method values and weights.
            JsonElement breakdown = GetProperty(root, "valuationMethods");
            breakdown.GetArrayLength().ShouldBe(3);
        }

        // ---- Competition factor end to end (SC-36 / SC-37) ------------------------------------

        [Fact]
        public async Task GetScore_WithNoCompetitorsAndNoBenchmark_ReportsCompetitionAsNoData()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Blank Competition Co");

            JsonElement root = await GetScoreAsync(CreateAuthenticatedClient(owner), startupId);

            // The old engine rewarded an empty list with the top of the scale (85).
            GetProperty(root, "competitionScore").ValueKind.ShouldBe(JsonValueKind.Null);

            JsonElement competition = Factor(root, "Competition");
            GetProperty(competition, "score").ValueKind.ShouldBe(JsonValueKind.Null);
            GetProperty(competition, "source").GetInt32().ShouldBe(0); // None — "no data"
            GetProperty(competition, "weight").GetDecimal().ShouldBe(0m);

            // The other four weights are renormalized back to 1.0.
            GetProperty(root, "factors").EnumerateArray()
                .Sum(f => GetProperty(f, "weight").GetDecimal())
                .ShouldBe(1.0m);

            // A dropped-out factor has no components to show, but the reader still sees what is
            // missing and what bringing it back would be worth.
            JsonElement detail = GetProperty(competition, "detail");
            GetProperty(detail, "components").GetArrayLength().ShouldBe(0);
            GetProperty(detail, "inputs").GetArrayLength().ShouldBeGreaterThan(0);

            JsonElement hint = GetProperty(detail, "hints").EnumerateArray().Single();
            GetProperty(hint, "code").GetString().ShouldBe("competition.hint.first_documented_card");
            GetProperty(hint, "enablesFactor").GetBoolean().ShouldBeTrue();
            // Neutral base 50 plus the first documented card (+10) — the score it would have, not a delta.
            GetProperty(hint, "points").GetDecimal().ShouldBe(60m);
        }

        // ---- Per-factor detail over the wire ---------------------------------------------------

        [Fact]
        public async Task GetScore_ReturnsPerFactorDetail_WithComponentsSummingToTheScore()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Detailed Co");

            JsonElement root = await GetScoreAsync(CreateAuthenticatedClient(owner), startupId);

            foreach (JsonElement factor in GetProperty(root, "factors").EnumerateArray())
            {
                string name = GetProperty(factor, "factor").GetString()!;
                JsonElement detail = GetProperty(factor, "detail");

                // Raw values only — the client owns units and locale, so nothing arrives pre-formatted.
                GetProperty(detail, "inputs").GetArrayLength().ShouldBeGreaterThan(0, name);

                JsonElement score = GetProperty(factor, "score");
                if (score.ValueKind == JsonValueKind.Null)
                {
                    continue;
                }

                GetProperty(detail, "components").EnumerateArray()
                    .Sum(c => GetProperty(c, "points").GetDecimal())
                    .ShouldBe(score.GetDecimal(), name);
            }

            // The Mvp seed startup fills value proposition and differentiators, so the product factor
            // shows the positioning bonus earned and advises the roadmap it has not filled in.
            JsonElement product = GetProperty(Factor(root, "Product"), "detail");
            GetProperty(product, "components").EnumerateArray()
                .Select(c => GetProperty(c, "code").GetString())
                .ShouldBe(["product.base.stage_mvp", "product.bonus.positioning"]);
            GetProperty(product, "hints").EnumerateArray()
                .Select(h => GetProperty(h, "code").GetString())
                .ShouldContain("product.hint.roadmap");

            // Enum-as-integer, like every other enum on the wire: 5 = Code, carrying "stage.mvp".
            JsonElement stage = GetProperty(product, "inputs").EnumerateArray()
                .Single(i => GetProperty(i, "code").GetString() == "product.input.stage");
            GetProperty(GetProperty(stage, "value"), "kind").GetInt32().ShouldBe(5);
            GetProperty(GetProperty(stage, "value"), "code").GetString().ShouldBe("stage.mvp");
        }

        [Fact]
        public async Task GetScore_NoLongerReturnsTheLegacyNotesArray()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "No Notes Co");

            JsonElement root = await GetScoreAsync(CreateAuthenticatedClient(owner), startupId);

            foreach (JsonElement factor in GetProperty(root, "factors").EnumerateArray())
            {
                factor.EnumerateObject()
                    .Select(p => p.Name.Replace("_", ""))
                    .ShouldNotContain(n => string.Equals(n, "notes", StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public async Task GetScore_RisesWithDocumentedCompetitors_AndFallsWhenOneIsDeleted()
        {
            User owner = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(owner);
            Guid startupId = await CreateStartupAsync(owner, "Honest Competition Co");

            Guid firstId = await AddCompetitorAsync(client, startupId, "Rival A", "https://rival-a.com");
            await AddCompetitorAsync(client, startupId, "Rival B", "https://rival-b.com");

            JsonElement documented = await GetScoreAsync(client, startupId);
            GetDecimal(documented, "competitionScore").ShouldBe(70m); // neutral base 50 + two documented
            decimal withTwo = GetDecimal(documented, "totalScore");

            (await client.DeleteAsync($"api/startup-competitors/{firstId}")).EnsureSuccessStatusCode();

            JsonElement afterDelete = await GetScoreAsync(client, startupId);
            GetDecimal(afterDelete, "competitionScore").ShouldBe(60m);
            // The point of the whole exercise: the "✕" button can no longer raise the score.
            GetDecimal(afterDelete, "totalScore").ShouldBeLessThan(withTwo);
        }

        [Fact]
        public async Task GetScore_UsesTheCompetitionIntensityBenchmark_AfterAnAdminAddsIt()
        {
            User owner = await SeedUserAsync();
            HttpClient ownerClient = CreateAuthenticatedClient(owner);
            Guid startupId = await CreateStartupAsync(owner, "Crowded Sector Co");

            await AddCompetitorAsync(ownerClient, startupId, "Rival A", "https://rival-a.com");

            // Warm the score cache so the benchmark write has something stale to evict.
            GetDecimal(await GetScoreAsync(ownerClient, startupId), "competitionScore").ShouldBe(60m);

            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpResponseMessage added = await CreateAuthenticatedClient(admin).PostAsJsonAsync(
                "api/admin/valuation-benchmarks",
                new
                {
                    metricType = (int)BenchmarkMetricType.CompetitionIntensity,
                    industry = (int)Industry.Saas,
                    stage = (int?)null,
                    value = 80m,
                    currency = (string?)null,
                    effectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    source = "Dsight 2025 Annual — SaaS competitive density",
                });
            added.StatusCode.ShouldBe(HttpStatusCode.OK);

            // Sector intensity 80 → base 20, plus the single documented card (+10).
            JsonElement afterBenchmark = await GetScoreAsync(ownerClient, startupId);
            GetDecimal(afterBenchmark, "competitionScore").ShouldBe(30m);

            // 1 (SelfReported) | 4 (ExternalBenchmark)
            GetProperty(Factor(afterBenchmark, "Competition"), "source").GetInt32().ShouldBe(5);
        }

        [Fact]
        public async Task AddBenchmark_WithAnOutOfRangeIntensity_IsRejected()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);

            HttpResponseMessage response = await CreateAuthenticatedClient(admin).PostAsJsonAsync(
                "api/admin/valuation-benchmarks",
                new
                {
                    metricType = (int)BenchmarkMetricType.CompetitionIntensity,
                    industry = (int)Industry.Saas,
                    stage = (int?)null,
                    value = 140m,
                    currency = (string?)null,
                    effectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    source = "out of range",
                });

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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

        private async Task<Guid> AddCompetitorAsync(HttpClient client, Guid startupId, string name, string website)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("api/startup-competitors", new
            {
                startup_id = startupId,
                name,
                website,
                description = (string?)null,
                // A website plus one of strengths/weaknesses makes the card count as documented.
                strengths_vs_us = "Larger sales team",
                weaknesses_vs_us = (string?)null,
            });
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            return await response.Content.ReadFromJsonAsync<Guid>();
        }

        private static async Task<JsonElement> GetScoreAsync(HttpClient client, Guid startupId)
        {
            HttpResponseMessage response = await client.GetAsync($"api/startups/{startupId}/score");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            // Clone so the element outlives the JsonDocument.
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.Clone();
        }

        private static JsonElement Factor(JsonElement root, string factorName) =>
            GetProperty(root, "factors")
                .EnumerateArray()
                .Single(f => GetProperty(f, "factor").GetString() == factorName);

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
