using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Domain.Admin;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Admin
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class AdminServiceOrdersTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private async Task<(ServiceOrder Order, Startup Startup)> SeedFulfilledPromotionAsync(Guid userId)
        {
            return await ExecuteDbAsync(async db =>
            {
                var startup = new Startup
                {
                    Id = Guid.NewGuid(),
                    Name = "Acme",
                    PublicEmail = "acme@devstart.test",
                    Stage = StartupStage.Seed,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                startup.Feature(30, DateTime.UtcNow);

                ServiceOrder order = ServiceOrder.CreatePending(
                    userId, ServiceType.Promotion, startup.Id, 1490m, "RUB", DateTime.UtcNow);
                order.MarkPaid(DateTime.UtcNow);
                order.MarkFulfilled(DateTime.UtcNow, accessDays: 30);

                db.Startups.Add(startup);
                db.ServiceOrders.Add(order);
                await db.SaveChangesAsync();
                return (order, startup);
            });
        }

        [Fact]
        public async Task GetServiceOrders_ListsOrdersForAnAdmin()
        {
            User buyer = await SeedUserAsync();
            User admin = await SeedUserAsync(UserSystemRole.Admin);
            (ServiceOrder order, _) = await SeedFulfilledPromotionAsync(buyer.Id);

            HttpResponseMessage response = await CreateAuthenticatedClient(admin)
                .GetAsync("api/admin/service-orders");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement item = doc.RootElement.EnumerateArray().Single();
            item.GetProperty("id").GetGuid().ShouldBe(order.Id);
            item.GetProperty("userEmail").GetString().ShouldBe(buyer.Email);
            item.GetProperty("isActive").GetBoolean().ShouldBeTrue();
        }

        [Fact]
        public async Task GetServiceOrders_ForANonAdmin_IsForbidden()
        {
            User user = await SeedUserAsync();

            (await CreateAuthenticatedClient(user).GetAsync("api/admin/service-orders"))
                .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Cancel_ClosesTheOrder_RevokesThePlacement_AndIsAudited()
        {
            User buyer = await SeedUserAsync();
            User admin = await SeedUserAsync(UserSystemRole.Admin);
            (ServiceOrder order, Startup startup) = await SeedFulfilledPromotionAsync(buyer.Id);

            HttpResponseMessage response = await CreateAuthenticatedClient(admin).PostAsJsonAsync(
                $"api/admin/service-orders/{order.Id}/cancel", new { reason = "проект удалён" });

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            await ExecuteDbAsync(async db =>
            {
                ServiceOrder stored = await db.ServiceOrders.SingleAsync(o => o.Id == order.Id);
                stored.Status.ShouldBe(ServiceOrderStatus.Cancelled);
                stored.GrantsAccess(DateTime.UtcNow).ShouldBeFalse();

                Startup storedStartup = await db.Startups.SingleAsync(s => s.Id == startup.Id);
                storedStartup.FeaturedUntil.ShouldBeNull();

                AdminActionLog log = await db.AdminActionLogs.SingleAsync(l => l.TargetId == order.Id);
                log.ActionType.ShouldBe(AdminActionType.CancelServiceOrder);
                log.TargetType.ShouldBe(AdminTargetType.ServiceOrder);
                log.AdminUserId.ShouldBe(admin.Id);
                log.Reason.ShouldBe("проект удалён");
            });
        }

        [Fact]
        public async Task Cancel_WithoutAReason_IsRejected()
        {
            User buyer = await SeedUserAsync();
            User admin = await SeedUserAsync(UserSystemRole.Admin);
            (ServiceOrder order, _) = await SeedFulfilledPromotionAsync(buyer.Id);

            HttpResponseMessage response = await CreateAuthenticatedClient(admin).PostAsJsonAsync(
                $"api/admin/service-orders/{order.Id}/cancel", new { reason = "" });

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            await ExecuteDbAsync(async db =>
                (await db.ServiceOrders.SingleAsync(o => o.Id == order.Id))
                    .Status.ShouldBe(ServiceOrderStatus.Fulfilled));
        }
    }
}
