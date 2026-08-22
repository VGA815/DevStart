using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Domain.PatentRegistry;
using DevStart.Domain.Registries;
using DevStart.Domain.StartupPatents;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Startups
{
    /// <summary>
    /// IP records and their resolution against the local register copy (SC-62…66).
    ///
    /// Two things are pinned here that nothing else can pin: the hygiene rules as the API actually
    /// enforces them, and the promise that a resolved record moves no number — the score and the
    /// valuation range are byte-identical before and after a record is checked against the register.
    /// </summary>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class StartupPatentTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private const string Endpoint = "api/startup-patents";
        private const string OwnerInn = "7707083893";
        private const string OtherInn = "7736207543";

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
            has_patents = true,
        };

        private static object PatentBody(Guid startupId, IntellectualPropertyKind kind, string number) => new
        {
            startup_id = startupId,
            kind = (int)kind,
            number,
        };

        private async Task<(HttpClient Client, Guid StartupId)> CreateStartupAsync(string name = "IP Co")
        {
            User owner = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(owner);
            HttpResponseMessage create = await client.PostAsJsonAsync(
                "api/startups/", CreateStartupBody(owner.Id, name));
            create.StatusCode.ShouldBe(HttpStatusCode.OK);
            return (client, await create.Content.ReadFromJsonAsync<Guid>());
        }

        private Task DeclareInnAsync(Guid startupId, string inn) => ExecuteDbAsync(async db =>
        {
            Domain.Startups.Startup startup = await db.Startups.SingleAsync(s => s.Id == startupId);
            startup.Inn = inn;
            await db.SaveChangesAsync();
        });

        private Task SeedRegistryAsync(
            IntellectualPropertyKind kind, string number, string? holderInn, string holder = "ООО «Ромашка»")
            => ExecuteDbAsync(async db =>
            {
                db.PatentRegistryEntries.Add(PatentRegistryEntry.Create(
                    kind, number, "Программа учёта", holder, holderInn,
                    new DateOnly(2023, 3, 15), PatentProtectionStatus.Active, "test", DateTime.UtcNow));
                await db.SaveChangesAsync();
            });

        [Fact]
        public async Task Create_StoresTheNormalizedNumber()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                Endpoint, PatentBody(startupId, IntellectualPropertyKind.Invention, "RU 2 731 234 C1"));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid id = await response.Content.ReadFromJsonAsync<Guid>();

            StartupPatent row = await ExecuteDbAsync(db => db.StartupPatents.SingleAsync(p => p.Id == id));

            // What the founder typed is kept; what the register is searched by is the digits.
            row.NumberRaw.ShouldBe("RU 2 731 234 C1");
            row.NumberNormalized.ShouldBe("2731234");
        }

        [Fact]
        public async Task Create_WithANumberThatDoesNotFitTheKind_IsRejectedAtInput()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            // A ten-digit certificate number filed as an invention: a typo at input, which must not be
            // allowed to surface later as "not found in the register" — that says something else.
            HttpResponseMessage response = await client.PostAsJsonAsync(
                Endpoint, PatentBody(startupId, IntellectualPropertyKind.Invention, "2023612345"));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ExecuteDbAsync(db => db.StartupPatents.AnyAsync())).ShouldBeFalse();
        }

        [Fact]
        public async Task Create_WithTheSameNumberTwice_ReturnsConflict()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            (await client.PostAsJsonAsync(
                    Endpoint, PatentBody(startupId, IntellectualPropertyKind.Invention, "2731234")))
                .EnsureSuccessStatusCode();

            // Same record behind different decoration — still the same record.
            HttpResponseMessage duplicate = await client.PostAsJsonAsync(
                Endpoint, PatentBody(startupId, IntellectualPropertyKind.Invention, "RU 2 731 234 C1"));

            duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await duplicate.Content.ReadAsStringAsync()).ShouldContain("StartupPatents.DuplicateNumber");
            (await ExecuteDbAsync(db => db.StartupPatents.CountAsync())).ShouldBe(1);
        }

        [Fact]
        public async Task Create_TheSameDigitsUnderAnotherKind_IsAllowed()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            (await client.PostAsJsonAsync(
                    Endpoint, PatentBody(startupId, IntellectualPropertyKind.ComputerProgram, "2023612345")))
                .EnsureSuccessStatusCode();

            // A program certificate and a database certificate are different objects in different
            // registers; the dedup key is (kind, number), not the digits alone.
            HttpResponseMessage other = await client.PostAsJsonAsync(
                Endpoint, PatentBody(startupId, IntellectualPropertyKind.Database, "2023612345"));

            other.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await ExecuteDbAsync(db => db.StartupPatents.CountAsync())).ShouldBe(2);
        }

        [Fact]
        public async Task Create_BeyondTheLimit_IsRejected()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync();

            // Filled directly: going through the API thirty times would only re-test HTTP.
            await ExecuteDbAsync(async db =>
            {
                for (int i = 0; i < StartupPatent.MaxPerStartup; i++)
                {
                    string number = $"20236{i:D5}";
                    db.StartupPatents.Add(StartupPatent.Create(
                        startupId, IntellectualPropertyKind.ComputerProgram, number, number, DateTime.UtcNow));
                }
                await db.SaveChangesAsync();
            });

            HttpResponseMessage response = await client.PostAsJsonAsync(
                Endpoint, PatentBody(startupId, IntellectualPropertyKind.Invention, "2731234"));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).ShouldContain("StartupPatents.LimitReached");
            (await ExecuteDbAsync(db => db.StartupPatents.CountAsync())).ShouldBe(StartupPatent.MaxPerStartup);
        }

        [Fact]
        public async Task Get_ShowsAllThreeStates_AndNeverHidesAMiss()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync("Three States Co");
            await DeclareInnAsync(startupId, OwnerInn);

            // Only the program register is loaded. The trademark register is not — a fact about the
            // platform, which must not read as a fact about the startup.
            await SeedRegistryAsync(IntellectualPropertyKind.ComputerProgram, "2023612345", OwnerInn);

            foreach ((IntellectualPropertyKind kind, string number) in new[]
            {
                (IntellectualPropertyKind.ComputerProgram, "2023612345"),
                (IntellectualPropertyKind.ComputerProgram, "2023619999"),
                (IntellectualPropertyKind.Trademark, "812345"),
            })
            {
                (await client.PostAsJsonAsync(Endpoint, PatentBody(startupId, kind, number)))
                    .EnsureSuccessStatusCode();
            }

            HttpResponseMessage response = await client.GetAsync($"api/startups/{startupId}/patents");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement root = doc.RootElement;

            root.GetProperty("hasPatentsDeclared").GetBoolean().ShouldBeTrue();
            root.GetProperty("declaredInn").GetString().ShouldBe(OwnerInn);

            JsonElement[] records = root.GetProperty("records").EnumerateArray().ToArray();
            records.Length.ShouldBe(3);

            JsonElement found = records.Single(r => r.GetProperty("numberNormalized").GetString() == "2023612345");
            found.GetProperty("state").GetInt32().ShouldBe((int)RegistryLookupState.Found);
            found.GetProperty("holderName").GetString().ShouldBe("ООО «Ромашка»");
            found.GetProperty("ownership").GetInt32()
                .ShouldBe((int)DeclaredValueComparison.Matches);

            // The miss is returned, not swallowed: a hidden non-match would make "enter twenty
            // numbers, show the three that stick" a working tactic.
            records.Single(r => r.GetProperty("numberNormalized").GetString() == "2023619999")
                .GetProperty("state").GetInt32().ShouldBe((int)RegistryLookupState.NotFoundInRegistry);

            records.Single(r => r.GetProperty("numberNormalized").GetString() == "812345")
                .GetProperty("state").GetInt32().ShouldBe((int)RegistryLookupState.RegistryUnavailable);

            // No ЕГРЮЛ source is wired in this host: the answer is "unavailable", never a finding.
            root.GetProperty("legalEntity").GetProperty("state").GetInt32().ShouldBe(0);
        }

        [Fact]
        public async Task Get_WithADifferentHolderInn_ReportsTheDifferenceRatherThanAMatch()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync("Other Holder Co");
            await DeclareInnAsync(startupId, OwnerInn);
            await SeedRegistryAsync(IntellectualPropertyKind.ComputerProgram, "2023612345", OtherInn, "АО «Мотор»");

            (await client.PostAsJsonAsync(
                    Endpoint, PatentBody(startupId, IntellectualPropertyKind.ComputerProgram, "2023612345")))
                .EnsureSuccessStatusCode();

            using JsonDocument doc = JsonDocument.Parse(
                await (await client.GetAsync($"api/startups/{startupId}/patents")).Content.ReadAsStringAsync());

            JsonElement record = doc.RootElement.GetProperty("records").EnumerateArray().Single();
            record.GetProperty("state").GetInt32().ShouldBe((int)RegistryLookupState.Found);
            record.GetProperty("ownership").GetInt32()
                .ShouldBe((int)DeclaredValueComparison.Differs);
        }

        [Fact]
        public async Task RegistryCheck_AddsTheProvenanceFlag_AndMovesNoNumber()
        {
            (HttpClient client, Guid startupId) = await CreateStartupAsync("Invariance Co");
            await DeclareInnAsync(startupId, OwnerInn);
            await SeedRegistryAsync(IntellectualPropertyKind.ComputerProgram, "2023612345", OwnerInn);

            (decimal Total, decimal Low, decimal High, decimal Point, string Methods, int Source) before =
                await ReadScoreAsync(client, startupId);
            (before.Source & 8).ShouldBe(0);

            (await client.PostAsJsonAsync(
                    Endpoint, PatentBody(startupId, IntellectualPropertyKind.ComputerProgram, "2023612345")))
                .EnsureSuccessStatusCode();

            (decimal Total, decimal Low, decimal High, decimal Point, string Methods, int Source) after =
                await ReadScoreAsync(client, startupId);

            // The whole point of the epic: provenance changes, numbers do not.
            (after.Source & 8).ShouldBe(8);
            after.Total.ShouldBe(before.Total);
            after.Low.ShouldBe(before.Low);
            after.High.ShouldBe(before.High);
            after.Point.ShouldBe(before.Point);
            after.Methods.ShouldBe(before.Methods);
        }

        private async Task<(decimal Total, decimal Low, decimal High, decimal Point, string Methods, int Source)>
            ReadScoreAsync(HttpClient client, Guid startupId)
        {
            HttpResponseMessage response = await client.GetAsync($"api/startups/{startupId}/score");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement root = doc.RootElement;

            JsonElement product = root.GetProperty("factors").EnumerateArray()
                .Single(f => f.GetProperty("factor").GetString() == "Product");

            return (
                root.GetProperty("totalScore").GetDecimal(),
                root.GetProperty("valuationLow").GetDecimal(),
                root.GetProperty("valuationHigh").GetDecimal(),
                root.GetProperty("valuationPoint").GetDecimal(),
                string.Join(',', root.GetProperty("methodsUsed").EnumerateArray().Select(m => m.GetString())),
                product.GetProperty("source").GetInt32());
        }
    }
}
