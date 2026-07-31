using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DevStart.Application.Abstractions.Payments;
using DevStart.Domain.Payments;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.ServiceOrders
{
    /// <summary>
    /// End-to-end delivery of a one-time service (SC-49): checkout → payment webhook → the buyer
    /// actually gets what they paid for, and a refund takes it back.
    /// </summary>
    [Collection(IntegrationTestCollection.Name)]
    public sealed class ServiceOrderDeliveryTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private const string ProviderPaymentId = "provider-pay-1";

        private async Task<Guid> SeedStartupAsync(Guid? founderProfileId = null)
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
                db.Startups.Add(startup);

                if (founderProfileId is Guid profileId)
                {
                    db.StartupMembers.Add(StartupMember.Create(
                        profileId, startup.Id, StartupRole.Founder, isPublic: true, DateTime.UtcNow));
                }

                await db.SaveChangesAsync();
                return startup.Id;
            });
        }

        /// <summary>Drives the payment-succeeded webhook the way YooKassa would.</summary>
        private async Task CapturePaymentAsync(HttpClient client)
        {
            Factory.PaymentProvider.WebhookToReturn = new PaymentWebhookEvent(
                WebhookEventKind.PaymentSucceeded, ProviderPaymentId);
            Factory.PaymentProvider.SnapshotToReturn = new ProviderPaymentSnapshot(
                ProviderPaymentId, PaymentStatus.Succeeded, Paid: true, DateTime.UtcNow,
                RefundedAmount: 0m, "succeeded");

            HttpResponseMessage webhook = await client.PostAsync(
                "api/webhooks/yookassa",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            webhook.IsSuccessStatusCode.ShouldBeTrue();
        }

        private async Task<Guid> CheckoutAsync(HttpClient client, ServiceType serviceType, Guid targetId)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/service-orders/checkout",
                new { serviceType = (int)serviceType, targetId });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("serviceOrderId").GetGuid();
        }

        [Fact]
        public async Task PaidScoringReport_UnlocksTheScore_ForThatStartupOnly_WithoutPro()
        {
            User buyer = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(buyer);
            Guid paidStartupId = await SeedStartupAsync();
            Guid otherStartupId = await SeedStartupAsync();

            // Before paying, an outside viewer without Pro is blocked.
            (await client.GetAsync($"api/startups/{paidStartupId}/score"))
                .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            await CheckoutAsync(client, ServiceType.ScoringReport, paidStartupId);
            await CapturePaymentAsync(client);

            (await client.GetAsync($"api/startups/{paidStartupId}/score"))
                .StatusCode.ShouldBe(HttpStatusCode.OK);

            // The entitlement is scoped: paying for one startup must not open another.
            (await client.GetAsync($"api/startups/{otherStartupId}/score"))
                .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            await ExecuteDbAsync(async db =>
            {
                ServiceOrder order = await db.ServiceOrders.SingleAsync(o => o.UserId == buyer.Id);
                order.Status.ShouldBe(ServiceOrderStatus.Fulfilled);
                order.TargetId.ShouldBe(paidStartupId);
                order.ExpiresAt.ShouldNotBeNull();
            });
        }

        [Fact]
        public async Task PaidPromotion_FeaturesTheStartup_AndListsItFirst()
        {
            User founder = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(founder);
            await SeedStartupAsync();
            Guid promotedId = await SeedStartupAsync(founderProfileId: founder.Id);

            await CheckoutAsync(client, ServiceType.Promotion, promotedId);
            await CapturePaymentAsync(client);

            await ExecuteDbAsync(async db =>
            {
                Startup startup = await db.Startups.SingleAsync(s => s.Id == promotedId);
                startup.FeaturedUntil.ShouldNotBeNull();
                startup.IsFeatured(DateTime.UtcNow).ShouldBeTrue();
            });

            HttpResponseMessage listing = await client.GetAsync("api/startups?page=1&pageSize=10");
            listing.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument doc = JsonDocument.Parse(await listing.Content.ReadAsStringAsync());
            JsonElement first = doc.RootElement.EnumerateArray().First();
            first.GetProperty("id").GetGuid().ShouldBe(promotedId);
            first.GetProperty("isFeatured").GetBoolean().ShouldBeTrue();
        }

        [Fact]
        public async Task Promotion_ForAStartupTheCallerDoesNotRun_IsRejected()
        {
            User outsider = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(outsider);
            Guid startupId = await SeedStartupAsync();

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/service-orders/checkout",
                new { serviceType = (int)ServiceType.Promotion, targetId = startupId });

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            await ExecuteDbAsync(async db =>
                (await db.ServiceOrders.AnyAsync(o => o.UserId == outsider.Id)).ShouldBeFalse());
        }

        [Fact]
        public async Task Checkout_WithoutTargetId_IsRejected()
        {
            User buyer = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(buyer);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/service-orders/checkout", new { serviceType = (int)ServiceType.ScoringReport });

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            await ExecuteDbAsync(async db =>
                (await db.ServiceOrders.AnyAsync(o => o.UserId == buyer.Id)).ShouldBeFalse());
        }

        [Fact]
        public async Task BuyingAccessTwiceIsRefused()
        {
            User buyer = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(buyer);
            Guid startupId = await SeedStartupAsync();

            await CheckoutAsync(client, ServiceType.ScoringReport, startupId);
            await CapturePaymentAsync(client);

            HttpResponseMessage second = await client.PostAsJsonAsync(
                "api/service-orders/checkout",
                new { serviceType = (int)ServiceType.ScoringReport, targetId = startupId });

            second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            await ExecuteDbAsync(async db =>
                (await db.ServiceOrders.CountAsync(o => o.UserId == buyer.Id)).ShouldBe(1));
        }

        [Fact]
        public async Task RefundingAPaidScoringReport_RevokesTheAccess()
        {
            User buyer = await SeedUserAsync();
            User admin = await SeedUserAsync(UserSystemRole.Admin);
            HttpClient client = CreateAuthenticatedClient(buyer);
            Guid startupId = await SeedStartupAsync();

            await CheckoutAsync(client, ServiceType.ScoringReport, startupId);
            await CapturePaymentAsync(client);
            (await client.GetAsync($"api/startups/{startupId}/score")).StatusCode.ShouldBe(HttpStatusCode.OK);

            Guid paymentId = await ExecuteDbAsync(async db =>
                (await db.Payments.SingleAsync(p => p.UserId == buyer.Id)).Id);

            HttpClient adminClient = CreateAuthenticatedClient(admin);
            HttpResponseMessage refund = await adminClient.PostAsJsonAsync(
                $"api/payments/{paymentId}/refund", new { });
            refund.IsSuccessStatusCode.ShouldBeTrue();

            (await client.GetAsync($"api/startups/{startupId}/score"))
                .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            await ExecuteDbAsync(async db =>
            {
                ServiceOrder order = await db.ServiceOrders.SingleAsync(o => o.UserId == buyer.Id);
                order.Status.ShouldBe(ServiceOrderStatus.Refunded);
                order.GrantsAccess(DateTime.UtcNow).ShouldBeFalse();
            });
        }

        [Fact]
        public async Task MyOrders_ListsTheBuyersOrdersWithTheirTarget()
        {
            User buyer = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(buyer);
            Guid startupId = await SeedStartupAsync();

            Guid orderId = await CheckoutAsync(client, ServiceType.ScoringReport, startupId);
            await CapturePaymentAsync(client);

            HttpResponseMessage response = await client.GetAsync("api/service-orders");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement item = doc.RootElement.EnumerateArray().Single();
            item.GetProperty("id").GetGuid().ShouldBe(orderId);
            item.GetProperty("targetId").GetGuid().ShouldBe(startupId);
            item.GetProperty("targetName").GetString().ShouldBe("Acme");
            item.GetProperty("isActive").GetBoolean().ShouldBeTrue();
            item.GetProperty("status").GetInt32().ShouldBe((int)ServiceOrderStatus.Fulfilled);
        }

        [Fact]
        public async Task MyOrders_WithoutToken_ReturnsUnauthorized()
        {
            (await CreateClient().GetAsync("api/service-orders"))
                .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}
