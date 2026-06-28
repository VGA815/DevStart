using System.Net;
using System.Net.Http.Json;
using DevStart.Application.Abstractions.Caching;
using DevStart.Application.Admin.Valuation;
using DevStart.Application.Scoring;
using DevStart.Domain.Admin;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.Domain.Valuation;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Admin
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class ValuationBenchmarkTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private const string Endpoint = "api/admin/valuation-benchmarks";

        // Numeric enum values as serialized over JSON (the API binds enums from numbers).
        private const int PreMoneyMedian = (int)BenchmarkMetricType.PreMoneyMedian; // 0
        private const int RevenueMultiple = (int)BenchmarkMetricType.RevenueMultiple; // 1
        private const int Saas = (int)Industry.Saas; // 1
        private const int Seed = (int)StartupStage.Seed; // 3

        private static object MedianBody(int industry, int stage, decimal value, DateTime effectiveFrom, string source = "Dsight 2025") => new
        {
            metricType = PreMoneyMedian,
            industry,
            stage = (int?)stage,
            value,
            currency = "RUB",
            effectiveFrom,
            source,
        };

        [Fact]
        public async Task AddBenchmark_AsAdmin_PersistsRow_IsReadable_AndWritesAuditLog()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpClient client = CreateAuthenticatedClient(admin);
            var effectiveFrom = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                Endpoint, MedianBody(Saas, Seed, 800_000_000m, effectiveFrom));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid id = await response.Content.ReadFromJsonAsync<Guid>();

            await ExecuteDbAsync(async db =>
            {
                ValuationBenchmark row = await db.ValuationBenchmarks.SingleAsync(b => b.Id == id);
                row.MetricType.ShouldBe(BenchmarkMetricType.PreMoneyMedian);
                row.Industry.ShouldBe(Industry.Saas);
                row.Stage.ShouldBe(StartupStage.Seed);
                row.Value.ShouldBe(800_000_000m);
                row.Currency.ShouldBe("RUB");
                row.CreatedByUserId.ShouldBe(admin.Id);

                (await db.AdminActionLogs.AnyAsync(l =>
                    l.ActionType == AdminActionType.AddValuationBenchmark
                    && l.TargetId == id
                    && l.AdminUserId == admin.Id)).ShouldBeTrue();
            });

            // Readable via the current-set endpoint.
            List<ValuationBenchmarkResponse>? current = await client.GetFromJsonAsync<List<ValuationBenchmarkResponse>>(Endpoint);
            current.ShouldNotBeNull();
            current.ShouldContain(b => b.Id == id && b.Value == 800_000_000m);
        }

        [Fact]
        public async Task AddBenchmark_AppendOnlyCorrection_NewVersionWins_AndHistoryKeepsBoth()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpClient client = CreateAuthenticatedClient(admin);

            var v1 = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var v2 = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

            (await client.PostAsJsonAsync(Endpoint, MedianBody(Saas, Seed, 500_000_000m, v1))).EnsureSuccessStatusCode();
            (await client.PostAsJsonAsync(Endpoint, MedianBody(Saas, Seed, 700_000_000m, v2))).EnsureSuccessStatusCode();

            // Current set (as of now) reflects the latest version.
            List<ValuationBenchmarkResponse>? current = await client.GetFromJsonAsync<List<ValuationBenchmarkResponse>>(Endpoint);
            ValuationBenchmarkResponse saasSeed = current!.Single(b =>
                b.MetricType == BenchmarkMetricType.PreMoneyMedian
                && b.Industry == Industry.Saas
                && b.Stage == StartupStage.Seed);
            saasSeed.Value.ShouldBe(700_000_000m);

            // History keeps both versions, newest first.
            List<ValuationBenchmarkResponse>? history = await client.GetFromJsonAsync<List<ValuationBenchmarkResponse>>(
                $"{Endpoint}/history?metricType={PreMoneyMedian}&industry={Saas}&stage={Seed}");
            history!.Count.ShouldBe(2);
            history[0].Value.ShouldBe(700_000_000m);
            history[1].Value.ShouldBe(500_000_000m);
        }

        [Fact]
        public async Task AddBenchmark_DuplicateVersion_ReturnsConflict()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpClient client = CreateAuthenticatedClient(admin);
            var effectiveFrom = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            (await client.PostAsJsonAsync(Endpoint, MedianBody(Saas, Seed, 500_000_000m, effectiveFrom))).EnsureSuccessStatusCode();

            HttpResponseMessage duplicate = await client.PostAsJsonAsync(
                Endpoint, MedianBody(Saas, Seed, 600_000_000m, effectiveFrom));

            duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task AddBenchmark_RevenueMultipleWithStage_ReturnsBadRequest()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpClient client = CreateAuthenticatedClient(admin);

            // A revenue multiple is sector-only — supplying a stage (and a currency) is invalid.
            HttpResponseMessage response = await client.PostAsJsonAsync(Endpoint, new
            {
                metricType = RevenueMultiple,
                industry = Saas,
                stage = (int?)Seed,
                value = 5m,
                currency = "RUB",
                effectiveFrom = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                source = "bad input",
            });

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AddBenchmark_AsRegularUser_ReturnsForbidden_AndPersistsNothing()
        {
            User regular = await SeedUserAsync(role: UserSystemRole.User);
            HttpClient client = CreateAuthenticatedClient(regular);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                Endpoint, MedianBody(Saas, Seed, 500_000_000m, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)));

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            bool any = await ExecuteDbAsync(db => db.ValuationBenchmarks.AnyAsync(b => b.Industry == Industry.Saas));
            any.ShouldBeFalse();
        }

        [Fact]
        public async Task AddBenchmark_Unauthenticated_ReturnsUnauthorized()
        {
            HttpResponseMessage response = await CreateClient().PostAsJsonAsync(
                Endpoint, MedianBody(Saas, Seed, 500_000_000m, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)));

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task AddBenchmark_InvalidatesBenchmarkCache()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpClient client = CreateAuthenticatedClient(admin);

            // Prime the cached benchmark set; the write path must evict it so the next valuation reloads.
            string key = CacheKeys.ValuationBenchmarks();
            await Factory.Cache.SetAsync(key, new List<ValuationBenchmarkRow>(), TimeSpan.FromMinutes(5));

            (await client.PostAsJsonAsync(
                Endpoint, MedianBody(Saas, Seed, 500_000_000m, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc))))
                .EnsureSuccessStatusCode();

            (await Factory.Cache.GetAsync<List<ValuationBenchmarkRow>>(key)).ShouldBeNull();
        }
    }
}
