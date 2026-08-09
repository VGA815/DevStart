using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Domain.Security;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using Shouldly;

namespace DevStart.IntegrationTests.Auth
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class TrustedDeviceTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private static string CodeFor(string base32Secret, int stepOffset = 0)
        {
            var totp = new Totp(Base32Encoding.ToBytes(base32Secret), step: 30, mode: OtpHashMode.Sha1, totpSize: 6);
            return totp.ComputeTotp(DateTime.UtcNow.AddSeconds(stepOffset * 30));
        }

        private static async Task<JsonElement> ReadRootAsync(HttpResponseMessage response)
        {
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.Clone();
        }

        private async Task<string> EnableTwoFactorAsync(User user)
        {
            HttpClient client = CreateAuthenticatedClient(user);

            HttpResponseMessage setup = await client.PostAsync("api/users/me/2fa/setup", content: null);
            setup.StatusCode.ShouldBe(HttpStatusCode.OK);
            string secret = (await ReadRootAsync(setup)).GetProperty("secret").GetString()!;

            HttpResponseMessage enable = await client.PostAsJsonAsync(
                "api/users/me/2fa/enable", new { code = CodeFor(secret) });
            enable.StatusCode.ShouldBe(HttpStatusCode.OK);

            return secret;
        }

        /// <summary>Logs in, answers the 2FA challenge asking to be remembered, and returns the device token.</summary>
        private async Task<string> LoginAndTrustDeviceAsync(HttpClient client, User user, string secret, int stepOffset = 1)
        {
            HttpResponseMessage login = await client.PostAsJsonAsync(
                "api/users/login", new { email = user.Email, password = DefaultPassword });
            login.StatusCode.ShouldBe(HttpStatusCode.OK);
            string pendingToken = (await ReadRootAsync(login))
                .GetProperty("twoFactor").GetProperty("pendingToken").GetString()!;

            HttpResponseMessage verify = await client.PostAsJsonAsync(
                "api/auth/2fa/verify",
                new { pending_token = pendingToken, code = CodeFor(secret, stepOffset), remember_device = true });

            verify.StatusCode.ShouldBe(HttpStatusCode.OK);
            JsonElement root = await ReadRootAsync(verify);
            root.GetProperty("tokens").ValueKind.ShouldBe(JsonValueKind.Object);

            JsonElement grant = root.GetProperty("trustedDevice");
            grant.ValueKind.ShouldBe(JsonValueKind.Object);
            return grant.GetProperty("deviceToken").GetString()!;
        }

        private async Task<JsonElement> LoginWithDeviceAsync(HttpClient client, User user, string? deviceToken)
        {
            HttpResponseMessage login = await client.PostAsJsonAsync(
                "api/users/login",
                new { email = user.Email, password = DefaultPassword, device_token = deviceToken });

            login.StatusCode.ShouldBe(HttpStatusCode.OK);
            return await ReadRootAsync(login);
        }

        [Fact]
        public async Task RememberedDevice_SkipsTheCodeOnTheNextLogin()
        {
            User user = await SeedUserAsync();
            string secret = await EnableTwoFactorAsync(user);
            HttpClient client = CreateClient();

            string deviceToken = await LoginAndTrustDeviceAsync(client, user, secret);

            JsonElement second = await LoginWithDeviceAsync(client, user, deviceToken);

            second.GetProperty("tokens").ValueKind.ShouldBe(JsonValueKind.Object);
            second.GetProperty("twoFactor").ValueKind.ShouldBe(JsonValueKind.Null);
        }

        [Fact]
        public async Task WithoutTheDeviceToken_TheCodeIsStillRequired()
        {
            User user = await SeedUserAsync();
            string secret = await EnableTwoFactorAsync(user);
            HttpClient client = CreateClient();

            await LoginAndTrustDeviceAsync(client, user, secret);

            JsonElement second = await LoginWithDeviceAsync(client, user, deviceToken: null);

            second.GetProperty("tokens").ValueKind.ShouldBe(JsonValueKind.Null);
            second.GetProperty("twoFactor").ValueKind.ShouldBe(JsonValueKind.Object);
        }

        [Fact]
        public async Task AnotherUsersDeviceToken_IsRejected()
        {
            User owner = await SeedUserAsync();
            User other = await SeedUserAsync();
            string ownerSecret = await EnableTwoFactorAsync(owner);
            await EnableTwoFactorAsync(other);

            HttpClient client = CreateClient();
            string deviceToken = await LoginAndTrustDeviceAsync(client, owner, ownerSecret);

            JsonElement result = await LoginWithDeviceAsync(client, other, deviceToken);

            result.GetProperty("twoFactor").ValueKind.ShouldBe(JsonValueKind.Object);
            // Presenting a token under the wrong account is treated as compromise, not a typo.
            bool ownerDeviceRevoked = await ExecuteDbAsync(db => db.TrustedDevices
                .AnyAsync(d => d.UserId == owner.Id && d.RevokedAt != null));
            ownerDeviceRevoked.ShouldBeTrue();
        }

        [Fact]
        public async Task SwitchingToEveryLogin_DropsTrustAndDemandsTheCodeAgain()
        {
            User user = await SeedUserAsync();
            string secret = await EnableTwoFactorAsync(user);
            HttpClient client = CreateClient();
            string deviceToken = await LoginAndTrustDeviceAsync(client, user, secret);

            HttpResponseMessage update = await CreateAuthenticatedClient(user).PutAsJsonAsync(
                "api/users/me/security",
                new { strictness = (int)TwoFactorStrictness.EveryLogin, trust_duration_days = 30, notify_on_new_device_login = true });
            update.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            JsonElement result = await LoginWithDeviceAsync(client, user, deviceToken);

            result.GetProperty("twoFactor").ValueKind.ShouldBe(JsonValueKind.Object);
            (await ExecuteDbAsync(db => db.TrustedDevices
                .CountAsync(d => d.UserId == user.Id && d.RevokedAt == null))).ShouldBe(0);
        }

        [Fact]
        public async Task ChangingThePassword_DropsTrustedDevices()
        {
            User user = await SeedUserAsync();
            string secret = await EnableTwoFactorAsync(user);
            HttpClient client = CreateClient();
            string deviceToken = await LoginAndTrustDeviceAsync(client, user, secret);

            const string newPassword = "NewPassword123!";
            HttpResponseMessage change = await CreateAuthenticatedClient(user).PostAsJsonAsync(
                "api/users/change-password",
                new { current_password = DefaultPassword, new_password = newPassword });
            change.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            HttpResponseMessage login = await client.PostAsJsonAsync(
                "api/users/login",
                new { email = user.Email, password = newPassword, device_token = deviceToken });

            login.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await ReadRootAsync(login)).GetProperty("twoFactor").ValueKind.ShouldBe(JsonValueKind.Object);
        }

        [Fact]
        public async Task RevokingTheDevice_DemandsTheCodeAgain()
        {
            User user = await SeedUserAsync();
            string secret = await EnableTwoFactorAsync(user);
            HttpClient client = CreateClient();
            string deviceToken = await LoginAndTrustDeviceAsync(client, user, secret);

            HttpClient authed = CreateAuthenticatedClient(user);
            HttpResponseMessage list = await authed.GetAsync("api/users/me/devices");
            list.StatusCode.ShouldBe(HttpStatusCode.OK);
            JsonElement devices = await ReadRootAsync(list);
            devices.GetArrayLength().ShouldBe(1);
            Guid deviceId = devices[0].GetProperty("id").GetGuid();

            HttpResponseMessage revoke = await authed.DeleteAsync($"api/users/me/devices/{deviceId}");
            revoke.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            JsonElement result = await LoginWithDeviceAsync(client, user, deviceToken);
            result.GetProperty("twoFactor").ValueKind.ShouldBe(JsonValueKind.Object);
        }

        [Fact]
        public async Task AnotherUsersDeviceId_Returns404()
        {
            User owner = await SeedUserAsync();
            User other = await SeedUserAsync();
            string secret = await EnableTwoFactorAsync(owner);
            await LoginAndTrustDeviceAsync(CreateClient(), owner, secret);

            Guid deviceId = await ExecuteDbAsync(db => db.TrustedDevices
                .Where(d => d.UserId == owner.Id).Select(d => d.Id).FirstAsync());

            HttpResponseMessage response = await CreateAuthenticatedClient(other)
                .DeleteAsync($"api/users/me/devices/{deviceId}");

            // NotFound, never Forbidden — a distinct code would confirm the id exists.
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AdminWithoutTwoFactor_MustStillEnroll_EvenWithADeviceToken()
        {
            // The admin enrolls, trusts this browser, then has 2FA reset by another admin.
            User admin = await SeedUserAsync(UserSystemRole.Admin);
            string secret = await EnableTwoFactorAsync(admin);
            HttpClient client = CreateClient();
            string deviceToken = await LoginAndTrustDeviceAsync(client, admin, secret);

            await ExecuteDbAsync(async db =>
            {
                await db.UserTwoFactors.Where(t => t.UserId == admin.Id).ExecuteDeleteAsync();
            });

            JsonElement result = await LoginWithDeviceAsync(client, admin, deviceToken);

            result.GetProperty("tokens").ValueKind.ShouldBe(JsonValueKind.Null);
            result.GetProperty("twoFactorSetup").ValueKind.ShouldBe(JsonValueKind.Object);
        }

        [Fact]
        public async Task SecuritySettings_RoundTrip_AndReportTheEffectiveCap()
        {
            User user = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(user);

            HttpResponseMessage defaults = await client.GetAsync("api/users/me/security");
            defaults.StatusCode.ShouldBe(HttpStatusCode.OK);
            JsonElement root = await ReadRootAsync(defaults);
            root.GetProperty("strictness").GetInt32().ShouldBe((int)TwoFactorStrictness.RememberDevice);
            root.GetProperty("maxTrustDurationDays").GetInt32().ShouldBeGreaterThan(0);
            root.GetProperty("availableDurations").GetArrayLength().ShouldBeGreaterThan(0);

            HttpResponseMessage update = await client.PutAsJsonAsync(
                "api/users/me/security",
                new { strictness = (int)TwoFactorStrictness.SameNetworkOnly, trust_duration_days = 14, notify_on_new_device_login = false });
            update.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            JsonElement after = await ReadRootAsync(await client.GetAsync("api/users/me/security"));
            after.GetProperty("strictness").GetInt32().ShouldBe((int)TwoFactorStrictness.SameNetworkOnly);
            after.GetProperty("trustDurationDays").GetInt32().ShouldBe(14);
            after.GetProperty("notifyOnNewDeviceLogin").GetBoolean().ShouldBeFalse();
        }

        [Fact]
        public async Task SecuritySettings_RejectADurationOutsideThePresets()
        {
            User user = await SeedUserAsync();

            HttpResponseMessage response = await CreateAuthenticatedClient(user).PutAsJsonAsync(
                "api/users/me/security",
                new { strictness = 1, trust_duration_days = 45, notify_on_new_device_login = true });

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData(99)]
        [InlineData(-1)]
        public async Task SecuritySettings_RejectAStrictnessOutsideTheEnum(int strictness)
        {
            // The endpoint casts the raw int; this proves the validation pipeline really does stop an
            // undefined value from reaching the database, end to end.
            User user = await SeedUserAsync();

            HttpResponseMessage response = await CreateAuthenticatedClient(user).PutAsJsonAsync(
                "api/users/me/security",
                new { strictness, trust_duration_days = 30, notify_on_new_device_login = true });

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ExecuteDbAsync(db => db.UserSecuritySettings.CountAsync(s => s.UserId == user.Id)))
                .ShouldBe(0);
        }
    }
}
