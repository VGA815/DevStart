using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevStart.Application.Abstractions.Authentication;
using DevStart.Domain.Admin;
using DevStart.Domain.ExternalLogins;
using DevStart.Domain.Users;
using DevStart.IntegrationTests.Infrastructure;
using OtpNet;
using Shouldly;

namespace DevStart.IntegrationTests.Auth
{
    [Collection(IntegrationTestCollection.Name)]
    public sealed class TwoFactorTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
    {
        private static object LoginBody(string email, string password) => new { email, password };

        /// <summary>Computes a real TOTP code for the secret returned by the setup endpoint.</summary>
        private static string CodeFor(string base32Secret, int stepOffset = 0)
        {
            var totp = new Totp(Base32Encoding.ToBytes(base32Secret), step: 30, mode: OtpHashMode.Sha1, totpSize: 6);
            return totp.ComputeTotp(DateTime.UtcNow.AddSeconds(stepOffset * 30));
        }

        /// <summary>A 6-digit code that is wrong at every step within ±2 of now (covers clock roll-over).</summary>
        private static string WrongCodeFor(string base32Secret)
        {
            var valid = Enumerable.Range(-2, 5).Select(o => CodeFor(base32Secret, o)).ToHashSet();
            for (int i = 0; ; i++)
            {
                string candidate = i.ToString("D6");
                if (!valid.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        private static async Task<JsonElement> ReadRootAsync(HttpResponseMessage response)
        {
            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.Clone();
        }

        private static async Task<string> ProblemTitleAsync(HttpResponseMessage response)
        {
            JsonElement root = await ReadRootAsync(response);
            return root.GetProperty("title").GetString()!;
        }

        /// <summary>Enrolls 2FA for <paramref name="user"/> through the real API and returns the secret + recovery codes.</summary>
        private async Task<(string Secret, List<string> RecoveryCodes)> EnableTwoFactorAsync(User user)
        {
            HttpClient client = CreateAuthenticatedClient(user);

            HttpResponseMessage setup = await client.PostAsync("api/users/me/2fa/setup", content: null);
            setup.StatusCode.ShouldBe(HttpStatusCode.OK);
            string secret = (await ReadRootAsync(setup)).GetProperty("secret").GetString()!;

            HttpResponseMessage enable = await client.PostAsJsonAsync(
                "api/users/me/2fa/enable", new { code = CodeFor(secret) });
            enable.StatusCode.ShouldBe(HttpStatusCode.OK);
            List<string> recoveryCodes = (await ReadRootAsync(enable)).GetProperty("recoveryCodes")
                .EnumerateArray().Select(e => e.GetString()!).ToList();

            return (secret, recoveryCodes);
        }

        private async Task<string> LoginExpectingChallengeAsync(HttpClient client, User user, string challengeProperty = "twoFactor")
        {
            HttpResponseMessage login = await client.PostAsJsonAsync(
                "api/users/login", LoginBody(user.Email, DefaultPassword));
            login.StatusCode.ShouldBe(HttpStatusCode.OK);

            JsonElement root = await ReadRootAsync(login);
            root.GetProperty("tokens").ValueKind.ShouldBe(JsonValueKind.Null);
            JsonElement challenge = root.GetProperty(challengeProperty);
            challenge.ValueKind.ShouldBe(JsonValueKind.Object);
            return challenge.GetProperty("pendingToken").GetString()!;
        }

        [Fact]
        public async Task FullLifecycle_SetupEnableLoginVerify_IssuesTokens()
        {
            User user = await SeedUserAsync();
            (string secret, List<string> recoveryCodes) = await EnableTwoFactorAsync(user);
            recoveryCodes.Count.ShouldBe(10);

            HttpClient client = CreateClient();
            string pendingToken = await LoginExpectingChallengeAsync(client, user);

            // +1 step: guaranteed newer than the timestep consumed by the enable call.
            HttpResponseMessage verify = await client.PostAsJsonAsync(
                "api/auth/2fa/verify", new { pending_token = pendingToken, code = CodeFor(secret, 1) });

            verify.StatusCode.ShouldBe(HttpStatusCode.OK);
            JsonElement root = await ReadRootAsync(verify);
            root.GetProperty("tokens").GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
            root.GetProperty("tokens").GetProperty("refreshToken").GetString().ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task ReplayedCode_IsRejectedOnSecondLogin()
        {
            User user = await SeedUserAsync();
            (string secret, _) = await EnableTwoFactorAsync(user);
            HttpClient client = CreateClient();

            string code = CodeFor(secret, 1);
            string firstToken = await LoginExpectingChallengeAsync(client, user);
            (await client.PostAsJsonAsync(
                "api/auth/2fa/verify", new { pending_token = firstToken, code }))
                .StatusCode.ShouldBe(HttpStatusCode.OK);

            string secondToken = await LoginExpectingChallengeAsync(client, user);
            HttpResponseMessage replay = await client.PostAsJsonAsync(
                "api/auth/2fa/verify", new { pending_token = secondToken, code });

            replay.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ProblemTitleAsync(replay)).ShouldBe("TwoFactor.InvalidCode");
        }

        [Fact]
        public async Task RecoveryCode_LogsIn_ButOnlyOnce()
        {
            User user = await SeedUserAsync();
            (_, List<string> recoveryCodes) = await EnableTwoFactorAsync(user);
            HttpClient client = CreateClient();

            string firstToken = await LoginExpectingChallengeAsync(client, user);
            HttpResponseMessage first = await client.PostAsJsonAsync(
                "api/auth/2fa/verify", new { pending_token = firstToken, code = recoveryCodes[0] });
            first.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await ReadRootAsync(first)).GetProperty("tokens").ValueKind.ShouldBe(JsonValueKind.Object);

            string secondToken = await LoginExpectingChallengeAsync(client, user);
            HttpResponseMessage reused = await client.PostAsJsonAsync(
                "api/auth/2fa/verify", new { pending_token = secondToken, code = recoveryCodes[0] });

            reused.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ProblemTitleAsync(reused)).ShouldBe("TwoFactor.InvalidCode");
        }

        [Fact]
        public async Task FifthWrongCode_KillsChallenge()
        {
            User user = await SeedUserAsync();
            (string secret, _) = await EnableTwoFactorAsync(user);
            HttpClient client = CreateClient();

            string pendingToken = await LoginExpectingChallengeAsync(client, user);
            string wrong = WrongCodeFor(secret);

            for (int attempt = 1; attempt <= 4; attempt++)
            {
                HttpResponseMessage response = await client.PostAsJsonAsync(
                    "api/auth/2fa/verify", new { pending_token = pendingToken, code = wrong });
                (await ProblemTitleAsync(response)).ShouldBe("TwoFactor.InvalidCode");
            }

            HttpResponseMessage fifth = await client.PostAsJsonAsync(
                "api/auth/2fa/verify", new { pending_token = pendingToken, code = wrong });
            (await ProblemTitleAsync(fifth)).ShouldBe("TwoFactor.TooManyAttempts");

            // Even the correct code is now useless — the challenge is gone; a fresh login is required.
            HttpResponseMessage after = await client.PostAsJsonAsync(
                "api/auth/2fa/verify", new { pending_token = pendingToken, code = CodeFor(secret, 1) });
            (await ProblemTitleAsync(after)).ShouldBe("TwoFactor.ChallengeExpired");
        }

        [Fact]
        public async Task Admin_WithoutTwoFactor_MustEnrollAtLogin()
        {
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpClient client = CreateClient();

            string pendingToken = await LoginExpectingChallengeAsync(client, admin, challengeProperty: "twoFactorSetup");

            HttpResponseMessage setup = await client.PostAsJsonAsync(
                "api/auth/2fa/setup", new { pending_token = pendingToken });
            setup.StatusCode.ShouldBe(HttpStatusCode.OK);
            JsonElement setupRoot = await ReadRootAsync(setup);
            string secret = setupRoot.GetProperty("secret").GetString()!;
            setupRoot.GetProperty("otpAuthUri").GetString()!.ShouldContain(secret);

            HttpResponseMessage confirm = await client.PostAsJsonAsync(
                "api/auth/2fa/setup/confirm", new { pending_token = pendingToken, code = CodeFor(secret) });
            confirm.StatusCode.ShouldBe(HttpStatusCode.OK);

            JsonElement confirmRoot = await ReadRootAsync(confirm);
            confirmRoot.GetProperty("recoveryCodes").GetArrayLength().ShouldBe(10);
            confirmRoot.GetProperty("auth").GetProperty("tokens")
                .GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();

            // Next login now demands a TOTP code, not enrollment.
            string nextToken = await LoginExpectingChallengeAsync(client, admin);
            nextToken.ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task OAuthLogin_WithTwoFactorEnabled_IsChallenged()
        {
            User user = await SeedUserAsync();
            (string secret, _) = await EnableTwoFactorAsync(user);
            await ExecuteDbAsync(db =>
            {
                db.ExternalLogins.Add(ExternalLogin.Create(
                    user.Id, ExternalLoginProvider.Google, "google-sub-2fa", user.Email, DateTime.UtcNow));
                return db.SaveChangesAsync();
            });

            Factory.GoogleAuth.Result = new ExternalUserInfo("google-sub-2fa", user.Email, true, "Test User", null);
            string state = "state-" + Guid.NewGuid().ToString("N");
            await Factory.OAuthStateStore.SaveAsync(
                state,
                new OAuthStateEntry(ExternalLoginProvider.Google, "verifier", "https://localhost/cb", null),
                TimeSpan.FromMinutes(5), default);

            HttpClient client = CreateClient();
            HttpResponseMessage callback = await client.GetAsync(
                $"api/auth/oauth/google/callback?code=any&state={state}");
            callback.StatusCode.ShouldBe(HttpStatusCode.OK);

            JsonElement root = await ReadRootAsync(callback);
            root.GetProperty("tokens").ValueKind.ShouldBe(JsonValueKind.Null);
            string pendingToken = root.GetProperty("twoFactor").GetProperty("pendingToken").GetString()!;

            HttpResponseMessage verify = await client.PostAsJsonAsync(
                "api/auth/2fa/verify", new { pending_token = pendingToken, code = CodeFor(secret, 1) });
            verify.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await ReadRootAsync(verify)).GetProperty("tokens").ValueKind.ShouldBe(JsonValueKind.Object);
        }

        [Fact]
        public async Task AdminReset_RemovesTwoFactor_AndWritesAudit()
        {
            User target = await SeedUserAsync();
            await EnableTwoFactorAsync(target);

            // The admin needs 2FA themselves to be realistic, but the reset endpoint only needs a
            // bearer token with the admin role — mint one directly.
            User admin = await SeedUserAsync(role: UserSystemRole.Admin);
            HttpClient adminClient = CreateAuthenticatedClient(admin);

            HttpResponseMessage reset = await adminClient.PostAsJsonAsync(
                $"api/admin/users/{target.Id}/2fa/reset", new { reason = "user lost the device" });
            reset.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            // 2FA rows are gone and the audit trail records the action.
            (await ExecuteDbAsync(db => Task.FromResult(db.UserTwoFactors.Count()))).ShouldBe(0);
            AdminActionLog audit = await ExecuteDbAsync(
                db => Task.FromResult(db.AdminActionLogs.Single(a => a.TargetId == target.Id)));
            audit.ActionType.ShouldBe(AdminActionType.ResetUserTwoFactor);
            audit.AdminUserId.ShouldBe(admin.Id);

            // The user now logs in without a challenge.
            HttpClient client = CreateClient();
            HttpResponseMessage login = await client.PostAsJsonAsync(
                "api/users/login", LoginBody(target.Email, DefaultPassword));
            login.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await ReadRootAsync(login)).GetProperty("tokens").ValueKind.ShouldBe(JsonValueKind.Object);
        }

        [Fact]
        public async Task NonAdmin_CannotResetTwoFactor()
        {
            User target = await SeedUserAsync();
            await EnableTwoFactorAsync(target);
            User rando = await SeedUserAsync();

            HttpResponseMessage reset = await CreateAuthenticatedClient(rando).PostAsJsonAsync(
                $"api/admin/users/{target.Id}/2fa/reset", new { reason = "not allowed" });

            reset.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UserResponse_ReflectsTwoFactorState()
        {
            User user = await SeedUserAsync();
            HttpClient client = CreateAuthenticatedClient(user);

            JsonElement before = await ReadRootAsync(await client.GetAsync($"api/users/{user.Id}"));
            before.GetProperty("twoFactorEnabled").GetBoolean().ShouldBeFalse();

            await EnableTwoFactorAsync(user);

            JsonElement after = await ReadRootAsync(await client.GetAsync($"api/users/{user.Id}"));
            after.GetProperty("twoFactorEnabled").GetBoolean().ShouldBeTrue();
        }

        [Fact]
        public async Task Enable_RevokesExistingSessions()
        {
            User user = await SeedUserAsync();

            // Establish a session through the real login flow.
            HttpClient client = CreateClient();
            HttpResponseMessage login = await client.PostAsJsonAsync(
                "api/users/login", LoginBody(user.Email, DefaultPassword));
            string refreshToken = (await ReadRootAsync(login))
                .GetProperty("tokens").GetProperty("refreshToken").GetString()!;

            await EnableTwoFactorAsync(user);

            // The pre-enable refresh token no longer rotates: sessions were revoked on enable.
            HttpResponseMessage refresh = await client.PostAsJsonAsync(
                "api/auth/refresh", new { refresh_token = refreshToken });
            refresh.StatusCode.ShouldNotBe(HttpStatusCode.OK);
        }
    }
}
