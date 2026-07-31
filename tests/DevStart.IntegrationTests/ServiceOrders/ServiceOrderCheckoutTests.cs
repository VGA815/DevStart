using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Domain.Payments;
using DevStart.Domain.ServiceOrders;
using DevStart.Domain.Startups;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.ServiceOrders
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class ServiceOrderCheckoutTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private async Task<Guid> SeedStartupAsync() => await ExecuteDbAsync(async db =>
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
            await db.SaveChangesAsync();
            return startup.Id;
        });

        [Fact]
        public async Task ServiceOrderCheckout_CreatesServiceOrderPayment_AndReturnsConfirmationUrl()
        {
            User user = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(user);
            Guid startupId = await SeedStartupAsync();

            // Body enums bind numerically (no JsonStringEnumConverter configured) — ScoringReport = 0.
            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/service-orders/checkout",
                new { serviceType = (int)ServiceType.ScoringReport, targetId = startupId });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using (JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
            {
                doc.RootElement.GetProperty("confirmationUrl").GetString()
                    .ShouldBe("https://pay.test.local/redirect");
            }

            await ExecuteDbAsync(async db =>
            {
                ServiceOrder order = await db.ServiceOrders.SingleAsync(o => o.UserId == user.Id);
                order.Status.ShouldBe(ServiceOrderStatus.Pending);
                order.ServiceType.ShouldBe(ServiceType.ScoringReport);
                order.TargetId.ShouldBe(startupId);
                order.Amount.ShouldBe(490.00m);

                Payment payment = await db.Payments.SingleAsync(p => p.UserId == user.Id);
                payment.Purpose.ShouldBe(PaymentPurpose.ServiceOrder);
                payment.ServiceOrderId.ShouldBe(order.Id);
                payment.SubscriptionId.ShouldBeNull();
                payment.Amount.ShouldBe(490.00m);
                payment.Status.ShouldBe(PaymentStatus.Pending);
            });
        }

        [Fact]
        public async Task ServiceOrderCheckout_WithoutToken_ReturnsUnauthorized()
        {
            HttpResponseMessage response = await CreateClient().PostAsJsonAsync(
                "api/service-orders/checkout",
                new { serviceType = (int)ServiceType.ScoringReport, targetId = Guid.NewGuid() });

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}
