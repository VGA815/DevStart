using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Application.UserConsents;
using DevStart.Domain.AccountDeletion;
using DevStart.Domain.Payments;
using DevStart.Domain.StartupMembers;
using DevStart.Domain.Startups;
using DevStart.Domain.Subscriptions;
using DevStart.Domain.UserConsents;
using DevStart.Domain.Users;
using DevStart.Infrastructure.AccountDeletion;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DevStart.IntegrationTests.Users
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class AccountDeletionTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private static object DeletionBody(string? password = DefaultPassword) => new { password };

        /// <summary>Runs the daily job the way Hangfire would.</summary>
        private async Task RunDeletionJobAsync()
        {
            using IServiceScope scope = Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AccountDeletionJob>().RunAsync(default);
        }

        private async Task ScheduleDueRequestAsync(Guid userId) =>
            await ExecuteDbAsync(async db =>
            {
                // Backdated so the grace window has already closed — the job is what this test is about,
                // not the wait.
                db.AccountDeletionRequests.Add(
                    AccountDeletionRequest.Create(userId, DateTime.UtcNow.AddDays(-8), TimeSpan.FromDays(7)));
                await db.SaveChangesAsync();
            });

        [Fact]
        public async Task Request_SchedulesTheErasure_AndTheStatusEndpointReportsIt()
        {
            User user = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(user);

            HttpResponseMessage response = await client.PostAsJsonAsync("api/users/me/deletion", DeletionBody());

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            body.RootElement.GetProperty("pending").GetBoolean().ShouldBeTrue();
            body.RootElement.GetProperty("scheduledFor").GetDateTime().ShouldBeGreaterThan(DateTime.UtcNow);

            HttpResponseMessage status = await client.GetAsync("api/users/me/deletion");
            status.StatusCode.ShouldBe(HttpStatusCode.OK);

            using JsonDocument statusBody = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
            statusBody.RootElement.GetProperty("pending").GetBoolean().ShouldBeTrue();

            Factory.EmailSender.AccountDeletionNotices.ShouldContain(n => n.Email == user.Email);
        }

        [Fact]
        public async Task Request_WithTheWrongPassword_IsRejected()
        {
            User user = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(user);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                "api/users/me/deletion", DeletionBody("not-the-password"));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ExecuteDbAsync(db => db.AccountDeletionRequests.AnyAsync())).ShouldBeFalse();
        }

        [Fact]
        public async Task Cancel_CallsOffAScheduledErasure()
        {
            User user = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(user);

            await client.PostAsJsonAsync("api/users/me/deletion", DeletionBody());

            HttpResponseMessage cancel = await client.DeleteAsync("api/users/me/deletion");
            cancel.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            HttpResponseMessage status = await client.GetAsync("api/users/me/deletion");
            using JsonDocument body = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
            body.RootElement.GetProperty("pending").GetBoolean().ShouldBeFalse();

            await RunDeletionJobAsync();

            (await ExecuteDbAsync(db => db.Users.AnyAsync(u => u.Id == user.Id))).ShouldBeTrue();
        }

        [Fact]
        public async Task TheJob_ErasesADueAccount_KeepsItsPayments_AndClosesOutTheRequest()
        {
            User user = await SeedUserAsync();

            await ExecuteDbAsync(async db =>
            {
                Subscription subscription = Subscription.CreatePending(user.Id, SubscriptionPlan.Pro, DateTime.UtcNow);
                db.Subscriptions.Add(subscription);
                db.Payments.Add(Payment.CreatePending(
                    user.Id, subscription.Id, PaymentProvider.YooKassa, 990m, "RUB", DateTime.UtcNow));
                await db.SaveChangesAsync();
            });

            await ScheduleDueRequestAsync(user.Id);

            await RunDeletionJobAsync();

            (await ExecuteDbAsync(db => db.Users.AnyAsync(u => u.Id == user.Id))).ShouldBeFalse();
            (await ExecuteDbAsync(db => db.Profiles.AnyAsync(p => p.UserId == user.Id))).ShouldBeFalse();
            (await ExecuteDbAsync(db => db.UserConsents.AnyAsync(c => c.UserId == user.Id))).ShouldBeFalse();

            // Retained on purpose: the privacy policy commits to holding payment records.
            (await ExecuteDbAsync(db => db.Payments.CountAsync(p => p.UserId == user.Id))).ShouldBe(1);

            AccountDeletionRequest request = await ExecuteDbAsync(db =>
                db.AccountDeletionRequests.SingleAsync(r => r.UserId == user.Id));
            request.Status.ShouldBe(AccountDeletionRequestStatus.Completed);
        }

        [Fact]
        public async Task TheJob_TakesTheSoleFoundersStartupWithIt()
        {
            User user = await SeedUserAsync();

            Guid startupId = await ExecuteDbAsync(async db =>
            {
                Startup startup = Startup.Create(
                    "solo", "solo@devstart.test", null, null, StartupStage.Idea, null, null, null,
                    DateTime.UtcNow, null, null);
                db.Startups.Add(startup);
                db.StartupMembers.Add(StartupMember.Create(
                    user.Id, startup.Id, StartupRole.Founder, true, DateTime.UtcNow));
                await db.SaveChangesAsync();
                return startup.Id;
            });

            await ScheduleDueRequestAsync(user.Id);

            await RunDeletionJobAsync();

            (await ExecuteDbAsync(db => db.Startups.AnyAsync(s => s.Id == startupId))).ShouldBeFalse();
            (await ExecuteDbAsync(db => db.StartupMembers.AnyAsync(m => m.StartupId == startupId))).ShouldBeFalse();
        }

        [Fact]
        public async Task ErasingABannedAccount_DoesNotOpenTheDoorToRegisteringAgain()
        {
            const string email = "banned.leaver@devstart.test";
            User user = await SeedUserAsync(email: email);

            await ExecuteDbAsync(async db =>
            {
                User target = await db.Users.SingleAsync(u => u.Id == user.Id);
                target.Ban("abuse", expiresAt: null, byUserId: Guid.NewGuid(), utcNow: DateTime.UtcNow);
                await db.SaveChangesAsync();
            });

            await ScheduleDueRequestAsync(user.Id);
            await RunDeletionJobAsync();

            (await ExecuteDbAsync(db => db.Users.AnyAsync(u => u.Id == user.Id))).ShouldBeFalse();

            HttpResponseMessage response = await CreateClient()
                .PostAsJsonAsync("api/users/register", RegisterBody(email, "banned_leaver"));

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await ExecuteDbAsync(db => db.Users.AnyAsync(u => u.Email == email))).ShouldBeFalse();
        }

        private static object RegisterBody(string email, string username) => new
        {
            email,
            username,
            password = DefaultPassword,
            bio = (string?)null,
            name = "Test User",
            url = (string?)null,
            social_media_links = Array.Empty<string>(),
            is_public = false,
            consents = new object[]
            {
                Consent(ConsentType.PersonalDataProcessing),
                Consent(ConsentType.PrivacyPolicy),
                Consent(ConsentType.TermsOfService),
                Consent(ConsentType.Cookies),
                Consent(ConsentType.PublicOffer),
            },
        };

        private static object Consent(ConsentType type) => new
        {
            type = (int)type,
            document_version = ConsentVersions.GetCurrentVersion(type),
            accepted = true,
        };
    }
}
