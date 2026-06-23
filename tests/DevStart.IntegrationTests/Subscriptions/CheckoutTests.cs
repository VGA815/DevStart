using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Domain.Payments;
using DevStart.Domain.PromoCodes;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Subscriptions
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class CheckoutTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        [Fact]
        public async Task Checkout_AsAuthenticatedUser_CreatesPendingPayment_AndReturnsConfirmationUrl()
        {
            User user = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(user);

            HttpResponseMessage response = await client.PostAsJsonAsync("api/subscriptions/checkout", new { });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using (JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
            {
                doc.RootElement.GetProperty("confirmationUrl").GetString()
                    .ShouldBe("https://pay.test.local/redirect");
                doc.RootElement.GetProperty("activated").GetBoolean().ShouldBeFalse();
            }

            // A pending payment (carrying the provider id/amount from the fake) and a pending subscription
            // should have been persisted.
            await ExecuteDbAsync(async db =>
            {
                Payment payment = await db.Payments.SingleAsync(p => p.UserId == user.Id);
                payment.Status.ShouldBe(PaymentStatus.Pending);
                payment.Amount.ShouldBe(990.00m);
                payment.ProviderPaymentId.ShouldBe("provider-pay-1");

                (await db.Subscriptions.AnyAsync(s =>
                    s.UserId == user.Id && s.Status == SubscriptionStatus.Pending)).ShouldBeTrue();
            });
        }

        [Fact]
        public async Task Checkout_WithoutToken_ReturnsUnauthorized()
        {
            HttpResponseMessage response = await CreateClient().PostAsJsonAsync("api/subscriptions/checkout", new { });

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Checkout_WithFreePromoCode_ActivatesPro_WithoutCallingTheProvider()
        {
            User user = await SeedUserAsync();
            await SeedFreePromoCodeAsync("FREEMONTH", days: 30);
            HttpClient client = CreateAuthenticatedClient(user);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/subscriptions/checkout", new { promoCode = "freemonth" }); // normalized to upper-case server-side

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using (JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
            {
                doc.RootElement.GetProperty("activated").GetBoolean().ShouldBeTrue();
            }

            await ExecuteDbAsync(async db =>
            {
                (await db.Subscriptions.AnyAsync(s =>
                    s.UserId == user.Id && s.Status == SubscriptionStatus.Active)).ShouldBeTrue();

                // The free path skips the payment provider entirely — no payment row is created.
                (await db.Payments.AnyAsync(p => p.UserId == user.Id)).ShouldBeFalse();
            });
        }

        private async Task SeedFreePromoCodeAsync(string code, int days)
        {
            await ExecuteDbAsync(async db =>
            {
                db.PromoCodes.Add(PromoCode.Create(
                    code: code,
                    discountType: PromoDiscountType.FreePeriod,
                    discountValue: 0m,
                    freePeriodDays: days,
                    plan: SubscriptionPlan.Pro,
                    maxRedemptions: null,
                    validFrom: null,
                    validUntil: null,
                    createdByUserId: Guid.NewGuid(),
                    utcNow: DateTime.UtcNow));
                await db.SaveChangesAsync();
            });
        }
    }
}
