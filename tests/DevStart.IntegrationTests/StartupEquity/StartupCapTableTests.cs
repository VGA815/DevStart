using System.Net;
using System.Net.Http.Json;
using DevStart.Application.StartupEquity.GetCapTable;
using DevStart.Domain.StartupEquity;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.StartupEquity
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class StartupCapTableTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private const int Founder = (int)EquityHolderType.Founder; // 0
        private const int Esop = (int)EquityHolderType.Esop;       // 1

        private static string Url(Guid startupId) => $"api/startups/{startupId}/cap-table";

        // Seeds a startup whose founder is the given user, returns the startup id.
        private async Task<Guid> SeedStartupWithFounderAsync(Guid founderProfileId, Guid? secondFounderProfileId = null)
        {
            Guid startupId = Guid.NewGuid();
            await ExecuteDbAsync(async db =>
            {
                DateTime now = DateTime.UtcNow;
                db.Startups.Add(new Startup
                {
                    Id = startupId,
                    Name = "Acme",
                    PublicEmail = "acme@example.com",
                    Stage = StartupStage.Seed,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                db.StartupMembers.Add(StartupMember.Create(founderProfileId, startupId, StartupRole.Founder, isPublic: true, now));
                if (secondFounderProfileId is { } second)
                {
                    db.StartupMembers.Add(StartupMember.Create(second, startupId, StartupRole.Founder, isPublic: true, now));
                }
                await db.SaveChangesAsync();
            });
            return startupId;
        }

        [Fact]
        public async Task SetThenGet_PersistsEquityAndVesting()
        {
            User founderA = await SeedUserAsync();
            User founderB = await SeedUserAsync();
            Guid startupId = await SeedStartupWithFounderAsync(founderA.Id, founderB.Id);
            HttpClient client = CreateAuthenticatedClient(founderA);

            var body = new
            {
                holders = new object[]
                {
                    new { holder_type = Founder, profile_id = founderA.Id, equity_percentage = 60m },
                    // Vesting starts in the future ⇒ nothing vested yet (deterministic without a fixed clock).
                    new { holder_type = Founder, profile_id = founderB.Id, equity_percentage = 30m,
                          vesting_start_date = DateTime.UtcNow.AddYears(1), vesting_months = 48, cliff_months = 12 },
                    new { holder_type = Esop, name = "ESOP pool", equity_percentage = 10m },
                }
            };

            HttpResponseMessage put = await client.PutAsJsonAsync(Url(startupId), body);
            put.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            // Persisted rows.
            await ExecuteDbAsync(async db =>
            {
                List<StartupEquityHolder> rows = await db.StartupEquityHolders
                    .Where(h => h.StartupId == startupId).ToListAsync();
                rows.Count.ShouldBe(3);
                rows.Single(r => r.ProfileId == founderA.Id).EquityPercentage.ShouldBe(60m);
                rows.Single(r => r.ProfileId == founderB.Id).VestingMonths.ShouldBe(48);
            });

            // Read model with computed vested amounts.
            StartupCapTableResponse? table = await client.GetFromJsonAsync<StartupCapTableResponse>(Url(startupId));
            table.ShouldNotBeNull();
            table.IsConfigured.ShouldBeTrue();
            table.TotalPercentage.ShouldBe(100m);
            table.Holders.Count.ShouldBe(3);
            table.Holders.Single(h => h.ProfileId == founderA.Id).VestedPercentage.ShouldBe(60m); // no schedule ⇒ vested
            table.Holders.Single(h => h.ProfileId == founderB.Id).VestedPercentage.ShouldBe(0m);  // future start ⇒ not vested
            table.TotalVestedPercentage.ShouldBe(70m);
        }

        [Fact]
        public async Task Get_WithoutExplicitTable_ReturnsBootstrappedDefault()
        {
            User founder = await SeedUserAsync();
            Guid startupId = await SeedStartupWithFounderAsync(founder.Id);
            HttpClient client = CreateAuthenticatedClient(founder);

            StartupCapTableResponse? table = await client.GetFromJsonAsync<StartupCapTableResponse>(Url(startupId));

            table.ShouldNotBeNull();
            table.IsConfigured.ShouldBeFalse();
            table.Holders.Count.ShouldBe(2); // single founder + ESOP
            table.Holders.Single(h => h.HolderType == EquityHolderType.Founder).EquityPercentage.ShouldBe(90m);
            table.Holders.Single(h => h.HolderType == EquityHolderType.Esop).EquityPercentage.ShouldBe(10m);
        }

        [Fact]
        public async Task Set_WithPercentagesNotSummingTo100_ReturnsBadRequest()
        {
            User founder = await SeedUserAsync();
            Guid startupId = await SeedStartupWithFounderAsync(founder.Id);
            HttpClient client = CreateAuthenticatedClient(founder);

            var body = new
            {
                holders = new object[]
                {
                    new { holder_type = Founder, profile_id = founder.Id, equity_percentage = 60m },
                    new { holder_type = Esop, name = "ESOP pool", equity_percentage = 10m },
                }
            };

            HttpResponseMessage put = await client.PutAsJsonAsync(Url(startupId), body);

            put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            bool any = await ExecuteDbAsync(db => db.StartupEquityHolders.AnyAsync(h => h.StartupId == startupId));
            any.ShouldBeFalse();
        }

        [Fact]
        public async Task Get_AsNonMember_ReturnsForbidden()
        {
            User founder = await SeedUserAsync();
            User outsider = await SeedUserAsync();
            Guid startupId = await SeedStartupWithFounderAsync(founder.Id);
            HttpClient client = CreateAuthenticatedClient(outsider);

            HttpResponseMessage response = await client.GetAsync(Url(startupId));

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Set_FounderRowForNonFounderProfile_ReturnsBadRequest()
        {
            User founder = await SeedUserAsync();
            User outsider = await SeedUserAsync();
            Guid startupId = await SeedStartupWithFounderAsync(founder.Id);
            HttpClient client = CreateAuthenticatedClient(founder);

            var body = new
            {
                holders = new object[]
                {
                    new { holder_type = Founder, profile_id = founder.Id, equity_percentage = 50m },
                    new { holder_type = Founder, profile_id = outsider.Id, equity_percentage = 40m },
                    new { holder_type = Esop, name = "ESOP pool", equity_percentage = 10m },
                }
            };

            HttpResponseMessage put = await client.PutAsJsonAsync(Url(startupId), body);

            put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
    }
}
