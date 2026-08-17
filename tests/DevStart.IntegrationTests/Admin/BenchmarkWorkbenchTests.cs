using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using DevStart.Application.Admin.Valuation;
using DevStart.Application.Admin.Valuation.GetBenchmarkSuggestions;
using DevStart.Application.Admin.Valuation.UploadDamodaranDataset;
using DevStart.Domain.Admin;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.Domain.Valuation;
using DevStart.IntegrationTests.Fakes;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Admin
{
    /// <summary>
    /// The workbench end to end over HTTP. The invariant under test throughout: none of these endpoints
    /// writes to <c>valuation_benchmark</c>. The only path there is still the add-benchmark command.
    /// </summary>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class BenchmarkWorkbenchTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private const string Root = "api/admin/valuation-benchmarks";
        private const int Saas = (int)Industry.Saas;
        private const int Ecommerce = (int)Industry.Ecommerce;

        /// <summary>
        /// A fixed valuation date passed to the endpoint, so the quarter the assertions are written
        /// against cannot shift under a run that straddles a quarter boundary.
        /// </summary>
        private static readonly DateTime AsOf = new(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime QuarterStart = BenchmarkQuarter.StartOf(AsOf);

        private static string AsOfQuery => $"asOf={Uri.EscapeDataString(AsOf.ToString("O"))}";

        private static object IssuerBody(
            string ticker,
            int industry = Saas,
            string? inn = null,
            bool isActive = true,
            decimal? revenueOverride = null,
            int? revenueOverrideFiscalYear = null,
            string? revenueOverrideNote = null) => new
            {
                id = (Guid?)null,
                ticker,
                inn,
                displayName = $"{ticker} Public Company",
                industry,
                isActive,
                revenueOverride,
                revenueOverrideFiscalYear,
                revenueOverrideNote,
                note = (string?)null,
            };

        private static MultipartFormDataContent CsvUpload(string csv, string fileName = "damodaran.csv")
        {
            var content = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
            file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            content.Add(file, "file", fileName);
            return content;
        }

        // ── Реестр ───────────────────────────────────────────────────────────────

        [Fact]
        public async Task SaveIssuer_AsAdmin_PersistsRow_AndWritesAuditLog()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpClient client = CreateAuthenticatedClient(admin);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Root}/issuers", IssuerBody("posi", inn: "7718668887"));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid id = await response.Content.ReadFromJsonAsync<Guid>();

            await ExecuteDbAsync(async db =>
            {
                BenchmarkIssuer issuer = await db.BenchmarkIssuers.SingleAsync(i => i.Id == id);
                issuer.Ticker.ShouldBe("POSI");            // normalised on the way in
                issuer.Industry.ShouldBe(Industry.Saas);
                issuer.IsActive.ShouldBeTrue();

                (await db.AdminActionLogs.AnyAsync(l =>
                    l.ActionType == AdminActionType.SaveBenchmarkIssuer
                    && l.TargetId == id
                    && l.AdminUserId == admin.Id)).ShouldBeTrue();
            });
        }

        [Fact]
        public async Task SaveIssuer_DuplicateTicker_ReturnsConflict()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            (await client.PostAsJsonAsync($"{Root}/issuers", IssuerBody("ASTR"))).EnsureSuccessStatusCode();

            HttpResponseMessage duplicate = await client.PostAsJsonAsync($"{Root}/issuers", IssuerBody("astr"));

            duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task SaveIssuer_MalformedInn_ReturnsBadRequest()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Root}/issuers", IssuerBody("DIAS", inn: "12345"));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SaveIssuer_RevenueOverrideWithoutYearOrNote_ReturnsBadRequest()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            // An override is the number that beats the collected one — it may not be unattributable.
            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Root}/issuers", IssuerBody("IVAT", revenueOverride: 1_000_000m));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SaveIssuer_AsRegularUser_ReturnsForbidden_AndPersistsNothing()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.User));

            HttpResponseMessage response = await client.PostAsJsonAsync($"{Root}/issuers", IssuerBody("SOFL"));

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await ExecuteDbAsync(db => db.BenchmarkIssuers.AnyAsync())).ShouldBeFalse();
        }

        [Fact]
        public async Task DeactivatingAnIssuer_KeepsTheRow_AndItsObservations()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            Guid id = await (await client.PostAsJsonAsync($"{Root}/issuers", IssuerBody("HEAD")))
                .Content.ReadFromJsonAsync<Guid>();

            await ExecuteDbAsync(async db =>
            {
                db.BenchmarkObservations.Add(BenchmarkObservation.ForIssuer(
                    BenchmarkObservationSource.Moex, id, BenchmarkObservationMetric.MarketCap,
                    100_000_000_000m, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                    fiscalYear: null, originNote: null, fetchedAt: DateTime.UtcNow));
                await db.SaveChangesAsync();
            });

            HttpResponseMessage response = await client.PostAsJsonAsync($"{Root}/issuers", new
            {
                id = (Guid?)id,
                ticker = "HEAD",
                inn = (string?)null,
                displayName = "HeadHunter",
                industry = (int)Industry.Marketplace,
                isActive = false,
                revenueOverride = (decimal?)null,
                revenueOverrideFiscalYear = (int?)null,
                revenueOverrideNote = (string?)null,
                note = "делистинг",
            });

            response.EnsureSuccessStatusCode();

            await ExecuteDbAsync(async db =>
            {
                BenchmarkIssuer issuer = await db.BenchmarkIssuers.SingleAsync(i => i.Id == id);
                issuer.IsActive.ShouldBeFalse();
                (await db.BenchmarkObservations.CountAsync(o => o.IssuerId == id)).ShouldBe(1);
            });
        }

        // ── Сопоставления ────────────────────────────────────────────────────────

        [Fact]
        public async Task SaveMapping_UpsertsByNaturalKey_AndCanRecordDeliberateExclusion()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            (await client.PostAsJsonAsync($"{Root}/industry-mappings", new
            {
                sourceKind = (int)BenchmarkMappingSourceKind.Damodaran,
                externalKey = "Retail (Online)",
                industry = (int?)Ecommerce,
                note = (string?)null,
            })).EnsureSuccessStatusCode();

            // Same key again: an edit, not a second row.
            (await client.PostAsJsonAsync($"{Root}/industry-mappings", new
            {
                sourceKind = (int)BenchmarkMappingSourceKind.Damodaran,
                externalKey = "Retail (Online)",
                industry = (int?)null,
                note = "не сопоставляется",
            })).EnsureSuccessStatusCode();

            List<BenchmarkIndustryMappingResponse>? mappings =
                await client.GetFromJsonAsync<List<BenchmarkIndustryMappingResponse>>($"{Root}/industry-mappings");

            BenchmarkIndustryMappingResponse mapping = mappings!.Single(m => m.ExternalKey == "Retail (Online)");
            mapping.Industry.ShouldBeNull();
            mapping.Note.ShouldBe("не сопоставляется");
        }

        [Fact]
        public async Task SaveMapping_MatchesTheKeyCaseInsensitively_SoACasingChangeIsAnEditNotADuplicate()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            Guid first = await (await client.PostAsJsonAsync($"{Root}/industry-mappings", new
            {
                sourceKind = (int)BenchmarkMappingSourceKind.Damodaran,
                externalKey = "Retail (Online)",
                industry = (int?)Ecommerce,
                note = (string?)null,
            })).Content.ReadFromJsonAsync<Guid>();

            // Every reader builds its lookup with OrdinalIgnoreCase, so two rows differing only in
            // casing would make those reads throw on a duplicate key. The same row must come back.
            Guid second = await (await client.PostAsJsonAsync($"{Root}/industry-mappings", new
            {
                sourceKind = (int)BenchmarkMappingSourceKind.Damodaran,
                externalKey = "RETAIL (online)",
                industry = (int?)Ecommerce,
                note = "регистр изменён",
            })).Content.ReadFromJsonAsync<Guid>();

            second.ShouldBe(first);

            List<BenchmarkIndustryMappingResponse> mappings =
                (await client.GetFromJsonAsync<List<BenchmarkIndustryMappingResponse>>($"{Root}/industry-mappings"))!;

            mappings.Count(m => string.Equals(m.ExternalKey, "Retail (Online)", StringComparison.OrdinalIgnoreCase))
                .ShouldBe(1);

            // The casing the admin last typed is what the UI shows.
            BenchmarkIndustryMappingResponse mapping = mappings.Single(m => m.Id == first);
            mapping.ExternalKey.ShouldBe("RETAIL (online)");
            mapping.Note.ShouldBe("регистр изменён");
        }

        [Fact]
        public async Task Suggestions_SurviveAMappingWhoseCasingDiffersFromTheStagedBucket()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            (await client.PostAsync(
                $"{Root}/damodaran?datasetYear=2025&datasetRegion=Emerging%20Markets",
                CsvUpload("Industry Name,EV/Sales\nRetail (Online),2.50\n"))).EnsureSuccessStatusCode();

            (await client.PostAsJsonAsync($"{Root}/industry-mappings", new
            {
                sourceKind = (int)BenchmarkMappingSourceKind.Damodaran,
                externalKey = "retail (online)",
                industry = (int?)Ecommerce,
                note = (string?)null,
            })).EnsureSuccessStatusCode();

            // The bucket is mapped, so it is off the queue — the join is case-insensitive on both sides.
            (await client.GetFromJsonAsync<List<UnmappedBenchmarkBucketResponse>>($"{Root}/unmapped-buckets"))
                !.ShouldBeEmpty();
        }

        [Fact]
        public async Task DeleteMapping_ReturnsMappedBucketToTheUnmappedQueue()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            (await client.PostAsync(
                $"{Root}/damodaran?datasetYear=2025&datasetRegion=Emerging%20Markets",
                CsvUpload("Industry Name,EV/Sales\nRetail (Online),2.50\n"))).EnsureSuccessStatusCode();

            Guid mappingId = await (await client.PostAsJsonAsync($"{Root}/industry-mappings", new
            {
                sourceKind = (int)BenchmarkMappingSourceKind.Damodaran,
                externalKey = "Retail (Online)",
                industry = (int?)Ecommerce,
                note = (string?)null,
            })).Content.ReadFromJsonAsync<Guid>();

            (await client.GetFromJsonAsync<List<UnmappedBenchmarkBucketResponse>>($"{Root}/unmapped-buckets"))
                !.ShouldBeEmpty();

            (await client.DeleteAsync($"{Root}/industry-mappings/{mappingId}")).StatusCode
                .ShouldBe(HttpStatusCode.NoContent);

            List<UnmappedBenchmarkBucketResponse>? unmapped =
                await client.GetFromJsonAsync<List<UnmappedBenchmarkBucketResponse>>($"{Root}/unmapped-buckets");
            unmapped!.Single().ExternalKey.ShouldBe("Retail (Online)");
        }

        // ── Загрузка Damodaran ───────────────────────────────────────────────────

        [Fact]
        public async Task UploadDataset_StoresOriginal_WritesObservations_AndAudits()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpClient client = CreateAuthenticatedClient(admin);

            HttpResponseMessage response = await client.PostAsync(
                $"{Root}/damodaran?datasetYear=2025&datasetRegion=Emerging%20Markets",
                CsvUpload("Industry Name,EV/Sales\nRetail (Online),2.50\nSoftware (System & Application),4.10\n"));

            response.EnsureSuccessStatusCode();
            UploadDamodaranDatasetResponse? result =
                await response.Content.ReadFromJsonAsync<UploadDamodaranDatasetResponse>();

            result!.BucketsImported.ShouldBe(2);
            result.ObjectKey.ShouldContain("damodaran/2025/");

            await ExecuteDbAsync(async db =>
            {
                List<BenchmarkObservation> observations = await db.BenchmarkObservations
                    .Where(o => o.Source == BenchmarkObservationSource.Damodaran)
                    .ToListAsync();

                observations.Count.ShouldBe(2);
                observations.ShouldAllBe(o => o.Metric == BenchmarkObservationMetric.EvSales);
                observations.ShouldAllBe(o => o.DatasetRegion == "Emerging Markets");
                observations.ShouldAllBe(o => o.AsOf.Year == 2025);

                (await db.AdminActionLogs.AnyAsync(l =>
                    l.ActionType == AdminActionType.UploadDamodaranDataset
                    && l.AdminUserId == admin.Id)).ShouldBeTrue();
            });
        }

        [Fact]
        public async Task UploadDataset_UnrecognisedLayout_ImportsNothingAtAll()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            HttpResponseMessage response = await client.PostAsync(
                $"{Root}/damodaran?datasetYear=2025&datasetRegion=Emerging%20Markets",
                CsvUpload("Sector Code,Ratio\nSW,3.45\n"));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

            // All or nothing: a partial import is worse than a refusal.
            (await ExecuteDbAsync(db => db.BenchmarkObservations.AnyAsync())).ShouldBeFalse();
        }

        [Fact]
        public async Task UploadDataset_SameYearTwice_ReplacesRatherThanMerges()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));
            string url = $"{Root}/damodaran?datasetYear=2025&datasetRegion=Emerging%20Markets";

            (await client.PostAsync(url, CsvUpload(
                "Industry Name,EV/Sales\nRetail (Online),2.50\nEducation,3.00\n"))).EnsureSuccessStatusCode();

            (await client.PostAsync(url, CsvUpload(
                "Industry Name,EV/Sales\nRetail (Online),9.90\n"))).EnsureSuccessStatusCode();

            await ExecuteDbAsync(async db =>
            {
                List<BenchmarkObservation> observations = await db.BenchmarkObservations
                    .Where(o => o.Source == BenchmarkObservationSource.Damodaran)
                    .ToListAsync();

                // The Education bucket from the first upload must be gone, not left behind as a blend.
                observations.Count.ShouldBe(1);
                observations[0].ExternalKey.ShouldBe("Retail (Online)");
                observations[0].Value.ShouldBe(9.90m);
            });
        }

        [Fact]
        public async Task UploadDataset_ADifferentRegionOfTheSameYear_LeavesTheFirstSliceAlone()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            (await client.PostAsync(
                $"{Root}/damodaran?datasetYear=2025&datasetRegion=Emerging%20Markets",
                CsvUpload("Industry Name,EV/Sales\nRetail (Online),2.50\n"))).EnsureSuccessStatusCode();

            (await client.PostAsync(
                $"{Root}/damodaran?datasetYear=2025&datasetRegion=Global",
                CsvUpload("Industry Name,EV/Sales\nRetail (Online),7.00\n"))).EnsureSuccessStatusCode();

            await ExecuteDbAsync(async db =>
            {
                List<BenchmarkObservation> observations = await db.BenchmarkObservations
                    .Where(o => o.Source == BenchmarkObservationSource.Damodaran)
                    .ToListAsync();

                // A replace is scoped to (year, region): the slice the derivation is not asking for must
                // not disappear silently under the one it is.
                observations.Count.ShouldBe(2);
                observations.Single(o => o.DatasetRegion == "Emerging Markets").Value.ShouldBe(2.50m);
                observations.Single(o => o.DatasetRegion == "Global").Value.ShouldBe(7.00m);
            });
        }

        [Fact]
        public async Task UploadDataset_OverTheSizeLimit_IsRejectedBeforeAnythingIsRead()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            var oversized = new StringBuilder("Industry Name,EV/Sales\n");
            while (oversized.Length <= UploadDamodaranDatasetCommand.MaxLengthBytes)
            {
                oversized.Append("Bucket ").Append(oversized.Length).Append(",2.50\n");
            }

            HttpResponseMessage response = await client.PostAsync(
                $"{Root}/damodaran?datasetYear=2025&datasetRegion=Emerging%20Markets",
                CsvUpload(oversized.ToString()));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ExecuteDbAsync(db => db.BenchmarkObservations.AnyAsync())).ShouldBeFalse();
        }

        [Fact]
        public async Task Suggestions_WhenTheRequestedRegionIsNotStaged_SayWhichSlicesAre()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            (await client.PostAsync(
                $"{Root}/damodaran?datasetYear=2025&datasetRegion=Global",
                CsvUpload("Industry Name,EV/Sales\nRetail (Online),6.00\n"))).EnsureSuccessStatusCode();

            (await client.PostAsJsonAsync($"{Root}/industry-mappings", new
            {
                sourceKind = (int)BenchmarkMappingSourceKind.Damodaran,
                externalKey = "Retail (Online)",
                industry = (int?)Ecommerce,
                note = (string?)null,
            })).EnsureSuccessStatusCode();

            BenchmarkSuggestionsResponse? result = await client.GetFromJsonAsync<BenchmarkSuggestionsResponse>(
                $"{Root}/suggestions?datasetRegion=Emerging%20Markets&{AsOfQuery}");

            BenchmarkSuggestionResponse ecommerce = result!.Suggestions.Single(s =>
                s.MetricType == BenchmarkMetricType.RevenueMultiple && s.Industry == Industry.Ecommerce);

            // The fix is a different parameter, not a mapping — the reason has to distinguish the two.
            ecommerce.Value.ShouldBeNull();
            ecommerce.NoSuggestionReason!.ShouldContain("Emerging Markets");
            ecommerce.NoSuggestionReason!.ShouldContain("Global");
        }

        [Fact]
        public async Task UploadDataset_AsRegularUser_ReturnsForbidden()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.User));

            HttpResponseMessage response = await client.PostAsync(
                $"{Root}/damodaran?datasetYear=2025&datasetRegion=Emerging%20Markets",
                CsvUpload("Industry Name,EV/Sales\nRetail (Online),2.50\n"));

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        // ── Предложения ──────────────────────────────────────────────────────────

        [Fact]
        public async Task Suggestions_AreReadOnly_AndWriteNoBenchmarkRow()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            int before = await ExecuteDbAsync(db => db.ValuationBenchmarks.CountAsync());

            BenchmarkSuggestionsResponse? result =
                await client.GetFromJsonAsync<BenchmarkSuggestionsResponse>($"{Root}/suggestions");

            result.ShouldNotBeNull();
            result.Suggestions.ShouldNotBeEmpty();
            (await ExecuteDbAsync(db => db.ValuationBenchmarks.CountAsync())).ShouldBe(before);
        }

        [Fact]
        public async Task Suggestions_EchoTheParametersTheyWereComputedWith()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            BenchmarkSuggestionsResponse? result = await client.GetFromJsonAsync<BenchmarkSuggestionsResponse>(
                $"{Root}/suggestions?minComparables=5&countryDiscount=0.4&illiquidityAndSizeDiscount=0.5"
                + "&datasetRegion=Global");

            result!.MinComparables.ShouldBe(5);
            result.CountryDiscount.ShouldBe(0.4m);
            result.IlliquidityAndSizeDiscount.ShouldBe(0.5m);
            result.DatasetRegion.ShouldBe("Global");
        }

        [Fact]
        public async Task Suggestions_WithNoStagingAtAll_SayThereIsNothingToSuggest()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            BenchmarkSuggestionsResponse? result =
                await client.GetFromJsonAsync<BenchmarkSuggestionsResponse>($"{Root}/suggestions");

            result!.HasObservations.ShouldBeFalse();

            foreach (BenchmarkSuggestionResponse suggestion in result.Suggestions
                .Where(s => s.MetricType == BenchmarkMetricType.RevenueMultiple))
            {
                suggestion.Value.ShouldBeNull();
                suggestion.NoSuggestionReason.ShouldNotBeNullOrWhiteSpace();
            }
        }

        [Fact]
        public async Task Suggestions_BuildARevenueMultipleFromStagedData_AndFlagAnExistingVersionAsACollision()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            (await client.PostAsync(
                $"{Root}/damodaran?datasetYear=2025&datasetRegion=Emerging%20Markets",
                CsvUpload("Industry Name,EV/Sales\nRetail (Online),6.00\n"))).EnsureSuccessStatusCode();

            (await client.PostAsJsonAsync($"{Root}/industry-mappings", new
            {
                sourceKind = (int)BenchmarkMappingSourceKind.Damodaran,
                externalKey = "Retail (Online)",
                industry = (int?)Ecommerce,
                note = (string?)null,
            })).EnsureSuccessStatusCode();

            Guid issuerId = await (await client.PostAsJsonAsync(
                $"{Root}/issuers", IssuerBody("OZON", industry: Ecommerce))).Content.ReadFromJsonAsync<Guid>();

            await ExecuteDbAsync(async db =>
            {
                db.BenchmarkObservations.Add(BenchmarkObservation.ForIssuer(
                    BenchmarkObservationSource.Moex, issuerId, BenchmarkObservationMetric.MarketCap,
                    300m, QuarterStart, null, null, AsOf));
                db.BenchmarkObservations.Add(BenchmarkObservation.ForIssuer(
                    BenchmarkObservationSource.GirBo, issuerId, BenchmarkObservationMetric.Revenue,
                    100m, QuarterStart, 2024, null, AsOf));

                // An existing version dated to this quarter must surface as a collision before the click.
                db.ValuationBenchmarks.Add(ValuationBenchmark.Create(
                    BenchmarkMetricType.RevenueMultiple, Industry.Ecommerce, stage: null, value: 1.40m,
                    currency: null, effectiveFrom: QuarterStart, source: "предыдущая версия",
                    createdByUserId: null, utcNow: QuarterStart));

                await db.SaveChangesAsync();
            });

            BenchmarkSuggestionsResponse? result = await client.GetFromJsonAsync<BenchmarkSuggestionsResponse>(
                $"{Root}/suggestions?minComparables=1&{AsOfQuery}");

            BenchmarkSuggestionResponse ecommerce = result!.Suggestions.Single(s =>
                s.MetricType == BenchmarkMetricType.RevenueMultiple && s.Industry == Industry.Ecommerce);

            // One comparable at 3.0× against a Damodaran base of 6.0 → coefficient 0.5, then × 0.70.
            ecommerce.Value.ShouldBe(2.10m);
            ecommerce.ComparableCount.ShouldBe(1);
            ecommerce.IsDerived.ShouldBeTrue();
            ecommerce.FiscalYears.ShouldBe([2024]);
            ecommerce.CurrentValue.ShouldBe(1.40m);
            ecommerce.DeltaPercent.ShouldBe(50.0m);
            ecommerce.CollidesWithExisting.ShouldBeTrue();
            ecommerce.EffectiveFrom.ShouldBe(QuarterStart);
            ecommerce.Source.ShouldNotBeNull();
            ecommerce.Source!.Length.ShouldBeLessThanOrEqualTo(512);
        }

        [Fact]
        public async Task Suggestions_UseTheRevenueOverrideRatherThanTheCollectedFigure()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            Guid issuerId = await (await client.PostAsJsonAsync($"{Root}/issuers", IssuerBody(
                "OZON",
                industry: Ecommerce,
                revenueOverride: 300m,
                revenueOverrideFiscalYear: 2023,
                revenueOverrideNote: "консолидированная МСФО, годовой отчёт"))).Content.ReadFromJsonAsync<Guid>();

            await ExecuteDbAsync(async db =>
            {
                db.BenchmarkObservations.Add(BenchmarkObservation.ForIssuer(
                    BenchmarkObservationSource.Moex, issuerId, BenchmarkObservationMetric.MarketCap,
                    300m, QuarterStart, null, null, AsOf));
                // РСБУ figure that would give a wildly different multiple; the override must win.
                db.BenchmarkObservations.Add(BenchmarkObservation.ForIssuer(
                    BenchmarkObservationSource.GirBo, issuerId, BenchmarkObservationMetric.Revenue,
                    10m, QuarterStart, 2024, null, AsOf));
                await db.SaveChangesAsync();
            });

            BenchmarkSuggestionsResponse? result = await client.GetFromJsonAsync<BenchmarkSuggestionsResponse>(
                $"{Root}/suggestions?minComparables=1&{AsOfQuery}");

            BenchmarkSuggestionResponse ecommerce = result!.Suggestions.Single(s =>
                s.MetricType == BenchmarkMetricType.RevenueMultiple && s.Industry == Industry.Ecommerce);

            // 300 / 300 = 1.0× (override), not 300 / 10 = 30×. Then × 0.70.
            ecommerce.Value.ShouldBe(0.70m);
            ecommerce.FiscalYears.ShouldBe([2023]);
        }

        [Fact]
        public async Task Suggestions_CoverCompetitionIntensityForEverySector()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.Admin));

            BenchmarkSuggestionsResponse? result =
                await client.GetFromJsonAsync<BenchmarkSuggestionsResponse>($"{Root}/suggestions");

            List<BenchmarkSuggestionResponse> intensities = result!.Suggestions
                .Where(s => s.MetricType == BenchmarkMetricType.CompetitionIntensity)
                .ToList();

            intensities.Count.ShouldBe(Enum.GetValues<Industry>().Length);
            intensities.ShouldAllBe(s => s.Value != null && s.Value >= 0m && s.Value <= 100m);
            intensities.ShouldAllBe(s => s.IsDerived == false);
        }

        [Fact]
        public async Task AcceptedCompetitionIntensity_PassesTheExistingAddCommand()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpClient client = CreateAuthenticatedClient(admin);

            BenchmarkSuggestionsResponse? suggestions =
                await client.GetFromJsonAsync<BenchmarkSuggestionsResponse>($"{Root}/suggestions");

            BenchmarkSuggestionResponse pick = suggestions!.Suggestions.First(s =>
                s.MetricType == BenchmarkMetricType.CompetitionIntensity && s.Industry == Industry.Saas);

            // Exactly what the UI's "Принять" hands to the existing form.
            HttpResponseMessage response = await client.PostAsJsonAsync(Root, new
            {
                metricType = (int)BenchmarkMetricType.CompetitionIntensity,
                industry = (int)pick.Industry,
                stage = (int?)null,
                value = pick.Value,
                currency = (string?)null,
                effectiveFrom = pick.EffectiveFrom,
                source = pick.Source,
            });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid id = await response.Content.ReadFromJsonAsync<Guid>();

            await ExecuteDbAsync(async db =>
            {
                ValuationBenchmark row = await db.ValuationBenchmarks.SingleAsync(b => b.Id == id);
                row.MetricType.ShouldBe(BenchmarkMetricType.CompetitionIntensity);
                row.Stage.ShouldBeNull();
                row.Currency.ShouldBeNull();
                // created_by stays the person answering for the number, which is the point of the detour.
                row.CreatedByUserId.ShouldBe(admin.Id);
            });
        }

        [Fact]
        public async Task Suggestions_AsRegularUser_ReturnsForbidden()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.User));

            (await client.GetAsync($"{Root}/suggestions")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        // ── Ручной запуск сбора ──────────────────────────────────────────────────

        [Fact]
        public async Task RunCollection_QueuesBothJobs_AndAudits()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpClient client = CreateAuthenticatedClient(admin);

            HttpResponseMessage response = await client.PostAsync($"{Root}/collect?kind=2", null);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            Factory.BackgroundJobs.BenchmarkCollections.ShouldContain(BenchmarkCollector.MarketCap);
            Factory.BackgroundJobs.BenchmarkCollections.ShouldContain(BenchmarkCollector.Revenue);

            (await ExecuteDbAsync(db => db.AdminActionLogs.AnyAsync(l =>
                l.ActionType == AdminActionType.RunBenchmarkCollection
                && l.AdminUserId == admin.Id))).ShouldBeTrue();
        }

        [Fact]
        public async Task RunCollection_AsRegularUser_ReturnsForbidden()
        {
            HttpClient client = CreateAuthenticatedClient(await SeedUserAsync(role: UserSystemRole.User));

            (await client.PostAsync($"{Root}/collect?kind=2", null)).StatusCode
                .ShouldBe(HttpStatusCode.Forbidden);
        }
    }
}
