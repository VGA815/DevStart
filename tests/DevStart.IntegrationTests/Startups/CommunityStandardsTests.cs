using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Domain.StartupCommunityStandards;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Startups
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class CommunityStandardsTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private const int CodeOfConduct = (int)CommunityDocumentType.CodeOfConduct;

        private static object CreateBody(Guid userId, string name) => new
        {
            user_id = userId,
            name,
            public_email = "founders@startup.test",
            description = "A startup building integration-tested things.",
            url = "https://startup.test",
            is_stopped = false,
            stage = 2,
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
            industry = 1,
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

        private static object DocumentBody(string title = "Кодекс поведения", string content = "Будьте людьми.")
            => new { title, content };

        [Fact]
        public async Task GetStandards_IsPublic_AndReportsDocumentsAsMissing()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Community Co");

            // No token: the checklist is a public trust signal.
            HttpResponseMessage response = await CreateClient().GetAsync($"api/startups/{startupId}/community");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement root = doc.RootElement;

            GetProperty(root, "totalCount").GetInt32().ShouldBe(12);

            JsonElement[] checks = GetProperty(root, "checks").EnumerateArray().ToArray();
            checks.Length.ShouldBe(12);

            // A freshly created startup has no community documents at all.
            checks.Where(c => GetProperty(c, "isDocument").GetBoolean())
                  .ShouldAllBe(c => !GetProperty(c, "isSatisfied").GetBoolean());

            // …but the profile fields supplied at creation already satisfy some rows.
            FindCheck(checks, "description").ShouldBeTrue();
            FindCheck(checks, "product").ShouldBeTrue();
            FindCheck(checks, "links").ShouldBeTrue();
            FindCheck(checks, "logo").ShouldBeFalse();
        }

        [Fact]
        public async Task UpsertDocument_AsFounder_SatisfiesTheChecklistRow()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Publishing Co");

            HttpResponseMessage put = await CreateAuthenticatedClient(owner)
                .PutAsJsonAsync($"api/startups/{startupId}/community/documents/{CodeOfConduct}", DocumentBody());

            put.StatusCode.ShouldBe(HttpStatusCode.OK);

            JsonElement[] checks = await GetChecksAsync(startupId);
            FindCheck(checks, "code_of_conduct").ShouldBeTrue();

            // The document itself is readable anonymously.
            HttpResponseMessage get = await CreateClient()
                .GetAsync($"api/startups/{startupId}/community/documents/{CodeOfConduct}");
            get.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument doc = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
            GetProperty(doc.RootElement, "content").GetString().ShouldBe("Будьте людьми.");
        }

        [Fact]
        public async Task UpsertDocument_Twice_ReplacesInPlace()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Rewriting Co");
            HttpClient client = CreateAuthenticatedClient(owner);

            HttpResponseMessage first = await client.PutAsJsonAsync(
                $"api/startups/{startupId}/community/documents/{CodeOfConduct}", DocumentBody());
            Guid firstId = await first.Content.ReadFromJsonAsync<Guid>();

            HttpResponseMessage second = await client.PutAsJsonAsync(
                $"api/startups/{startupId}/community/documents/{CodeOfConduct}",
                DocumentBody(content: "Вторая редакция."));
            Guid secondId = await second.Content.ReadFromJsonAsync<Guid>();

            // One document per type: the second write updates the first rather than adding a row.
            secondId.ShouldBe(firstId);

            int count = await ExecuteDbAsync(db => db.StartupCommunityDocuments
                .CountAsync(d => d.StartupId == startupId));
            count.ShouldBe(1);
        }

        [Fact]
        public async Task UpsertDocument_AsOutsider_IsForbidden()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Guarded Co");

            User outsider = await SeedUserAsync();
            HttpResponseMessage response = await CreateAuthenticatedClient(outsider)
                .PutAsJsonAsync($"api/startups/{startupId}/community/documents/{CodeOfConduct}", DocumentBody());

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task DeleteDocument_ReturnsTheChecklistRowToUnsatisfied()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Retracting Co");
            HttpClient client = CreateAuthenticatedClient(owner);

            await client.PutAsJsonAsync(
                $"api/startups/{startupId}/community/documents/{CodeOfConduct}", DocumentBody());
            FindCheck(await GetChecksAsync(startupId), "code_of_conduct").ShouldBeTrue();

            HttpResponseMessage delete = await client
                .DeleteAsync($"api/startups/{startupId}/community/documents/{CodeOfConduct}");
            delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            FindCheck(await GetChecksAsync(startupId), "code_of_conduct").ShouldBeFalse();
        }

        [Fact]
        public async Task GetStandards_ForBannedStartup_IsNotFound()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Banned Co");

            // Warm the cache first, so this also proves the ban gate is not sitting behind it.
            (await CreateClient().GetAsync($"api/startups/{startupId}/community"))
                .StatusCode.ShouldBe(HttpStatusCode.OK);

            await ExecuteDbAsync(async db =>
            {
                Domain.Startups.Startup startup = await db.Startups.SingleAsync(s => s.Id == startupId);
                startup.Ban("spam", expiresAt: null, byUserId: owner.Id, utcNow: DateTime.UtcNow);
                await db.SaveChangesAsync();
            });

            HttpResponseMessage response = await CreateClient().GetAsync($"api/startups/{startupId}/community");
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetTemplates_ReturnsAStarterTextForEveryDocumentType()
        {
            User user = await SeedUserAsync();

            HttpResponseMessage response = await CreateAuthenticatedClient(user)
                .GetAsync("api/community-standards/templates");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement[] templates = doc.RootElement.EnumerateArray().ToArray();

            templates.Length.ShouldBe(Enum.GetValues<CommunityDocumentType>().Length);
            templates.ShouldAllBe(t => GetProperty(t, "content").GetString()!.Length > 0);
            templates.ShouldAllBe(t => GetProperty(t, "title").GetString()!.Length > 0);
        }

        [Fact]
        public async Task Catalog_ExposesTheBadgeAndFiltersByLevel()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Catalog Co");

            // Publishing a document refreshes the projection the catalog reads.
            await CreateAuthenticatedClient(owner).PutAsJsonAsync(
                $"api/startups/{startupId}/community/documents/{CodeOfConduct}", DocumentBody());

            JsonElement[] all = await GetCatalogAsync(string.Empty);
            JsonElement entry = all.Single(s => GetProperty(s, "id").GetGuid() == startupId);
            GetProperty(entry, "communityStandardsPercent").GetDecimal().ShouldBeGreaterThan(0m);
            GetProperty(entry, "communityStandardsLevel").GetInt32()
                .ShouldBe((int)CommunityStandardsLevel.Incomplete);

            // The startup is nowhere near Complete, so a filter on it excludes the startup.
            JsonElement[] filtered = await GetCatalogAsync(
                $"&minCommunityStandards={(int)CommunityStandardsLevel.Complete}");
            filtered.ShouldNotContain(s => GetProperty(s, "id").GetGuid() == startupId);
        }

        [Fact]
        public async Task Catalog_WithoutAProjectionRow_IsExcludedByAnAboveIncompleteFilter()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Unevaluated Co");

            // Nothing has triggered a refresh, so this startup has no projection row at all.
            bool hasRow = await ExecuteDbAsync(db => db.StartupCommunityStandards
                .AnyAsync(cs => cs.StartupId == startupId));
            hasRow.ShouldBeFalse();

            JsonElement[] filtered = await GetCatalogAsync(
                $"&minCommunityStandards={(int)CommunityStandardsLevel.Developing}");

            filtered.ShouldNotContain(s => GetProperty(s, "id").GetGuid() == startupId);
        }

        [Fact]
        public async Task Catalog_WithoutAProjectionRow_IsStillIncludedByAnIncompleteFilter()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Unevaluated Too Co");

            // The listing reports an unevaluated startup as Incomplete, so a filter for Incomplete must
            // keep it — otherwise the filter would contradict the badge shown next to it.
            JsonElement[] filtered = await GetCatalogAsync(
                $"&minCommunityStandards={(int)CommunityStandardsLevel.Incomplete}");

            JsonElement entry = filtered.Single(s => GetProperty(s, "id").GetGuid() == startupId);
            GetProperty(entry, "communityStandardsLevel").GetInt32()
                .ShouldBe((int)CommunityStandardsLevel.Incomplete);
            GetProperty(entry, "communityStandardsPercent").GetDecimal().ShouldBe(0m);
        }

        [Fact]
        public async Task Catalog_AtDevelopingLevel_PassesDevelopingButNotCompleteFilter()
        {
            User owner = await SeedUserAsync();
            Guid startupId = await CreateStartupAsync(owner, "Developing Co");
            HttpClient client = CreateAuthenticatedClient(owner);

            // Creation already satisfies description/links/product; three documents take it to six of
            // twelve, which is exactly the Developing threshold.
            foreach (CommunityDocumentType type in new[]
            {
                CommunityDocumentType.CodeOfConduct,
                CommunityDocumentType.Contributing,
                CommunityDocumentType.Support
            })
            {
                HttpResponseMessage put = await client.PutAsJsonAsync(
                    $"api/startups/{startupId}/community/documents/{(int)type}", DocumentBody());
                put.StatusCode.ShouldBe(HttpStatusCode.OK);
            }

            await ExecuteDbAsync(async db =>
            {
                StartupCommunityStandards row = await db.StartupCommunityStandards
                    .SingleAsync(cs => cs.StartupId == startupId);
                row.CompletedCount.ShouldBe(6);
                row.Level.ShouldBe(CommunityStandardsLevel.Developing);
            });

            JsonElement[] developing = await GetCatalogAsync(
                $"&minCommunityStandards={(int)CommunityStandardsLevel.Developing}");
            developing.ShouldContain(s => GetProperty(s, "id").GetGuid() == startupId);

            JsonElement[] complete = await GetCatalogAsync(
                $"&minCommunityStandards={(int)CommunityStandardsLevel.Complete}");
            complete.ShouldNotContain(s => GetProperty(s, "id").GetGuid() == startupId);
        }

        private async Task<JsonElement[]> GetCatalogAsync(string extraQuery)
        {
            HttpResponseMessage response = await CreateClient()
                .GetAsync($"api/startups?page=1&pageSize=50{extraQuery}");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            // Cloned so the elements stay usable once the document is disposed.
            return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToArray();
        }

        private async Task<JsonElement[]> GetChecksAsync(Guid startupId)
        {
            HttpResponseMessage response = await CreateClient().GetAsync($"api/startups/{startupId}/community");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return GetProperty(doc.RootElement, "checks").EnumerateArray().Select(e => e.Clone()).ToArray();
        }

        private static bool FindCheck(IEnumerable<JsonElement> checks, string key) =>
            GetProperty(
                checks.Single(c => GetProperty(c, "key").GetString() == key),
                "isSatisfied").GetBoolean();

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
    }
}
