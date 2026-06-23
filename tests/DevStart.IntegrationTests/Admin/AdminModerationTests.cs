using System.Net;
using System.Net.Http.Json;
using DevStart.Domain.Admin;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DevStart.IntegrationTests.Admin
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class AdminModerationTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        [Fact]
        public async Task BanUser_AsAdmin_BansTarget_AndWritesAuditLog()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            User target = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(admin);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"api/admin/users/{target.Id}/ban", new { reason = "Spamming the platform", expiresAt = (DateTime?)null });

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            await ExecuteDbAsync(async db =>
            {
                User reloaded = await db.Users.SingleAsync(u => u.Id == target.Id);
                reloaded.IsBanned.ShouldBeTrue();

                (await db.AdminActionLogs.AnyAsync(l =>
                    l.ActionType == AdminActionType.BanUser
                    && l.TargetId == target.Id
                    && l.AdminUserId == admin.Id)).ShouldBeTrue();
            });
        }

        [Fact]
        public async Task BanUser_AsRegularUser_ReturnsForbidden()
        {
            User regular = await SeedUserAsync(role: UserSystemRole.User);
            User target = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(regular);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"api/admin/users/{target.Id}/ban", new { reason = "I am not allowed", expiresAt = (DateTime?)null });

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            bool banned = await ExecuteDbAsync(db => db.Users.AnyAsync(u => u.Id == target.Id && u.IsBanned));
            banned.ShouldBeFalse();
        }

        [Fact]
        public async Task BanUser_Unauthenticated_ReturnsUnauthorized()
        {
            User target = await SeedUserAsync();

            HttpResponseMessage response = await CreateClient().PostAsJsonAsync(
                $"api/admin/users/{target.Id}/ban", new { reason = "no token", expiresAt = (DateTime?)null });

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GrantSubscription_AsAdmin_ActivatesPro_AndWritesAuditLog()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            User target = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(admin);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/admin/subscriptions/grant",
                new { userId = target.Id, durationDays = (int?)30, reason = "Community contributor" });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid subscriptionId = await response.Content.ReadFromJsonAsync<Guid>();

            await ExecuteDbAsync(async db =>
            {
                Subscription subscription = await db.Subscriptions.SingleAsync(s => s.Id == subscriptionId);
                subscription.UserId.ShouldBe(target.Id);
                subscription.Status.ShouldBe(SubscriptionStatus.Active);
                subscription.Source.ShouldBe(SubscriptionSource.AdminGrant);

                (await db.AdminActionLogs.AnyAsync(l =>
                    l.ActionType == AdminActionType.GrantSubscription
                    && l.AdminUserId == admin.Id)).ShouldBeTrue();
            });
        }

        [Fact]
        public async Task CreatePromoCode_AsAdmin_PersistsNormalizedCode()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpClient client = CreateAuthenticatedClient(admin);

            HttpResponseMessage response = await client.PostAsJsonAsync("api/admin/promo-codes", new
            {
                code = "summer25",
                discountType = 0, // Percentage
                discountValue = 25m,
                freePeriodDays = (int?)null,
                plan = 1, // Pro
                maxRedemptions = (int?)100,
                validFrom = (DateTime?)null,
                validUntil = (DateTime?)null,
            });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            Guid promoId = await response.Content.ReadFromJsonAsync<Guid>();

            await ExecuteDbAsync(async db =>
            {
                DevStart.Domain.PromoCodes.PromoCode promo = await db.PromoCodes.SingleAsync(p => p.Id == promoId);
                promo.Code.ShouldBe("SUMMER25"); // normalized to upper-case
                promo.DiscountValue.ShouldBe(25m);
            });
        }
    }
}
