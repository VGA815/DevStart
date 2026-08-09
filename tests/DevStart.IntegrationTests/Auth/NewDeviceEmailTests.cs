using System.Net;
using System.Net.Http.Json;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Shouldly;

namespace DevStart.IntegrationTests.Auth
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class NewDeviceEmailTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private const string Chrome =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";
        private const string SafariMac =
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Safari/605.1.15";

        private async Task LoginAsync(User user, string userAgent)
        {
            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);

            HttpResponseMessage login = await client.PostAsJsonAsync(
                "api/users/login", new { email = user.Email, password = DefaultPassword });

            login.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        private int QueuedEmailsFor(string email)
            => Factory.BackgroundJobs.NewDeviceLoginEmails.Count(e => e.Email == email);

        [Fact]
        public async Task FirstEverLogin_SendsNothing()
        {
            User user = await SeedUserAsync();

            await LoginAsync(user, Chrome);

            // The user is at the keyboard having just registered; warning them about themselves is noise.
            QueuedEmailsFor(user.Email).ShouldBe(0);
        }

        [Fact]
        public async Task LoginFromAnUnseenBrowser_QueuesOneEmail()
        {
            User user = await SeedUserAsync();
            await LoginAsync(user, Chrome);

            await LoginAsync(user, SafariMac);

            QueuedEmailsFor(user.Email).ShouldBe(1);
            var queued = Factory.BackgroundJobs.NewDeviceLoginEmails.Single(e => e.Email == user.Email);
            queued.Browser.ShouldBe("Safari");
            queued.Os.ShouldBe("macOS");
        }

        [Fact]
        public async Task RepeatLoginFromTheSameBrowser_SendsNothing()
        {
            User user = await SeedUserAsync();
            await LoginAsync(user, Chrome);
            await LoginAsync(user, Chrome);

            await LoginAsync(user, Chrome);

            QueuedEmailsFor(user.Email).ShouldBe(0);
        }

        [Fact]
        public async Task DisablingTheNotification_SuppressesTheEmail()
        {
            User user = await SeedUserAsync();
            await LoginAsync(user, Chrome);

            HttpResponseMessage update = await CreateAuthenticatedClient(user).PutAsJsonAsync(
                "api/users/me/security",
                new { strictness = 1, trust_duration_days = 30, notify_on_new_device_login = false });
            update.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            await LoginAsync(user, SafariMac);

            QueuedEmailsFor(user.Email).ShouldBe(0);
        }
    }
}
