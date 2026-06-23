using System.Net.Http.Headers;
using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.UserConsents;
using DevStart.Domain.Profiles;
using DevStart.Domain.UserConsents;
using DevStart.Domain.UserPreferences;
using DevStart.Domain.Users;
using DevStart.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevStart.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Base class for HTTP-level integration tests. Resets the database before each test, hands out
    /// rate-limit-isolated <see cref="HttpClient"/>s, and provides helpers to seed users and mint JWTs
    /// (so authenticated endpoints can be hit without going through the full login/verify/consent flow).
    /// </summary>
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        public const string DefaultPassword = "Password123!";

        private static int _ipCounter = 0x0A000001; // 10.0.0.1, incremented per test for a unique partition.

        private protected readonly IntegrationTestWebAppFactory Factory;
        private readonly string _clientIp;

        protected IntegrationTestBase(IntegrationTestWebAppFactory factory)
        {
            Factory = factory;
            int n = Interlocked.Increment(ref _ipCounter);
            _clientIp = $"{(n >> 24) & 0xFF}.{(n >> 16) & 0xFF}.{(n >> 8) & 0xFF}.{n & 0xFF}";
        }

        public async Task InitializeAsync() => await Factory.ResetDatabaseAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        /// <summary>An anonymous client whose requests share this test's dedicated rate-limit partition.</summary>
        protected HttpClient CreateClient()
        {
            HttpClient client = Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
            client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.HeaderName, _clientIp);
            return client;
        }

        /// <summary>A client carrying a Bearer token for <paramref name="user"/>.</summary>
        protected HttpClient CreateAuthenticatedClient(User user)
        {
            HttpClient client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(user));
            return client;
        }

        protected string CreateToken(User user)
        {
            using IServiceScope scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<ITokenProvider>().CreateAccessToken(user);
        }

        protected async Task<T> ExecuteDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
        {
            using IServiceScope scope = Factory.Services.CreateScope();
            ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await action(db);
        }

        protected async Task ExecuteDbAsync(Func<ApplicationDbContext, Task> action)
        {
            using IServiceScope scope = Factory.Services.CreateScope();
            ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await action(db);
        }

        /// <summary>
        /// Inserts a user directly. By default the user is verified and has accepted all mandatory consents
        /// at their active versions, so it can log in immediately; pass flags to seed the negative cases.
        /// </summary>
        protected async Task<User> SeedUserAsync(
            UserSystemRole role = UserSystemRole.User,
            bool verified = true,
            bool acceptMandatoryConsents = true,
            string password = DefaultPassword,
            string? email = null)
        {
            using IServiceScope scope = Factory.Services.CreateScope();
            ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            IPasswordHasher hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            string suffix = Guid.NewGuid().ToString("N")[..8];
            var user = User.Create(
                username: $"user_{suffix}",
                email: email ?? $"user_{suffix}@devstart.test",
                passwordHash: hasher.Hash(password),
                createdAt: DateTime.UtcNow);
            user.IsVerified = verified;
            user.Role = role;

            db.Users.Add(user);

            // Mirror the registration flow: a user always has a profile (startup membership FKs to it) and
            // a preferences row.
            db.Profiles.Add(Profile.Create(user.Id, "Test User", bio: null, url: null,
                isAvailableForHire: false, isPublic: true, avatarId: null));
            db.Preferences.Add(UserPreference.Create(user.Id, UserPreferenceTheme.System));

            if (acceptMandatoryConsents)
            {
                Dictionary<ConsentType, string> active = await db.ConsentDocuments
                    .Where(d => d.IsActive)
                    .ToDictionaryAsync(d => d.Type, d => d.Version);

                foreach (ConsentType type in ConsentVersions.MandatoryTypes)
                {
                    if (active.TryGetValue(type, out string? version))
                    {
                        db.UserConsents.Add(UserConsent.Create(user.Id, type, version, DateTime.UtcNow));
                    }
                }
            }

            await db.SaveChangesAsync();
            return user;
        }
    }
}
