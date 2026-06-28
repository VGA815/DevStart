using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.BackgroundJobs;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Application.Abstractions.Payments;
using DevStart.Domain.ExternalLogins;
using DevStart.Domain.Startups;
using DevStart.Domain.Valuation;
using DevStart.Infrastructure.Database;
using DevStart.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace DevStart.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Boots the real WebApi pipeline (<see cref="Program"/>) against a throwaway PostgreSQL container,
    /// with all external services (Redis, MinIO, YooKassa, Centrifugo, SMTP, Hangfire scheduling, OAuth
    /// providers) swapped for in-memory fakes. EF migrations and the startup seeders run as in production,
    /// so endpoints are exercised end-to-end over HTTP with a genuine database underneath.
    /// </summary>
    public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<DevStart.WebApi.Program>, IAsyncLifetime
    {
#pragma warning disable CS0618 // PostgreSqlBuilder() is obsolete; the image is set below via WithImage.
        private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
            // Use an image already present locally (the dev stack pulls postgres:latest; 17-alpine is cached)
            // to keep cold-start fast and avoid a registry pull during CI.
            .WithImage("postgres:17-alpine")
            .WithDatabase("devstart_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
#pragma warning restore CS0618

        private Respawner? _respawner;
        private NpgsqlConnection? _resetConnection;

        // Typed handles to the fakes so tests can configure inputs and assert recorded calls.
        internal InMemoryCacheService Cache { get; } = new();
        internal RecordingEmailSender EmailSender { get; } = new();
        internal FakeFileStorage FileStorage { get; } = new();
        internal RecordingNotificationSender NotificationSender { get; } = new();
        internal FakePaymentProvider PaymentProvider { get; } = new();
        internal RecordingBackgroundJobScheduler BackgroundJobs { get; } = new();
        internal InMemoryOAuthStateStore OAuthStateStore { get; } = new();
        internal InMemoryPendingRegistrationStore PendingRegistrations { get; } = new();
        internal FakeExternalAuthProvider GoogleAuth { get; } = new() { Provider = ExternalLoginProvider.Google };
        internal FakeExternalAuthProvider GitHubAuth { get; } = new() { Provider = ExternalLoginProvider.GitHub };

        public async Task InitializeAsync()
        {
            await _database.StartAsync();

            // Configuration is supplied via environment variables (not ConfigureAppConfiguration) because the
            // minimal-hosting Program reads connection strings and options during service registration —
            // before the WebApplicationFactory's app-configuration callbacks would run. Environment variables
            // are picked up eagerly by WebApplication.CreateBuilder, so they are in place in time.
            foreach ((string key, string value) in TestConfiguration())
            {
                Environment.SetEnvironmentVariable(key.Replace(":", "__"), value);
            }
        }

        private Dictionary<string, string> TestConfiguration() => new()
        {
            ["ConnectionStrings:Database"] = _database.GetConnectionString(),
            ["Database:RunMigrationsOnStartup"] = "true",

            // Quiet logging: turn the configured Seq sink into a console sink so nothing tries to reach Seq.
            ["Serilog:MinimumLevel:Default"] = "Warning",
            ["Serilog:WriteTo:1:Name"] = "Console",

            ["Jwt:Secret"] = "integration-tests-super-secret-signing-key-0123456789",
            ["Jwt:Issuer"] = "devstart-tests",
            ["Jwt:Audience"] = "devstart-tests",
            ["Jwt:ExpirationInMinutes"] = "60",
            ["Jwt:RequireHttpsMetadata"] = "false",
            ["Jwt:RefreshToken:LifetimeDays"] = "30",

            // ValidateOnStart requires non-empty OAuth credentials even though the providers are faked.
            ["OAuth:Google:ClientId"] = "test-google-client",
            ["OAuth:Google:ClientSecret"] = "test-google-secret",
            ["OAuth:Google:RedirectUri"] = "https://localhost/api/auth/oauth/google/callback",
            ["OAuth:GitHub:ClientId"] = "test-github-client",
            ["OAuth:GitHub:ClientSecret"] = "test-github-secret",
            ["OAuth:GitHub:RedirectUri"] = "https://localhost/api/auth/oauth/github/callback",

            // ValidateOnStart for YooKassa: ShopId/SecretKey/ReturnUrl required, ApiUrl absolute.
            ["YooKassa:ShopId"] = "test-shop",
            ["YooKassa:SecretKey"] = "test-secret",
            ["YooKassa:ApiUrl"] = "https://api.yookassa.test",
            ["YooKassa:ReturnUrl"] = "https://localhost/billing/return",
            ["YooKassa:VerifyWebhookIp"] = "false",
            ["YooKassa:Receipt:Enabled"] = "true",
            ["YooKassa:Receipt:VatCode"] = "1",

            ["Plans:Pro:Price"] = "990.00",
            ["Plans:Pro:Currency"] = "RUB",
            ["Plans:Pro:DurationDays"] = "30",
            ["Plans:Pro:Description"] = "DevStart Pro — 30 days",

            ["Billing:ReconcileMinAgeMinutes"] = "10",
            ["Billing:ReconcileMaxAgeHours"] = "72",
            ["Billing:ReminderDaysBefore"] = "3",

            // AddSmtp reads/parses these eagerly at registration time, so they must be present & valid.
            ["Smtp:Host"] = "localhost",
            ["Smtp:Port"] = "25",
            ["Smtp:EnableSsl"] = "false",
            ["Smtp:UseDefaultCredentials"] = "false",
            ["Smtp:Username"] = "tests@devstart.local",
            ["Smtp:Password"] = "unused",

            // Bound but unused — the Redis/MinIO/Centrifugo clients are never resolved (services faked).
            ["Redis:ConnectionString"] = "localhost:6379,abortConnect=false",
            ["Redis:InstanceName"] = "tests",
            ["Minio:Endpoint"] = "localhost:9000",
            ["Minio:AccessKey"] = "test",
            ["Minio:SecretKey"] = "test",
            ["Minio:Bucket"] = "files",
            ["Minio:UseSsl"] = "false",
            ["Minio:PubEndpoint"] = "localhost:9000",
            ["Minio:PubUseSsl"] = "false",
            ["Centrifugo:ApiUrl"] = "http://localhost:8000",
            ["Centrifugo:ApiKey"] = "test-api-key",
            ["Centrifugo:TokenHmacSecret"] = "integration-tests-centrifugo-hmac-secret-0123456789",
            ["Centrifugo:TokenExpirationInMinutes"] = "10",
            ["Frontend:BaseUrl"] = "https://localhost",
        };

        public new async Task DisposeAsync()
        {
            if (_resetConnection is not null)
            {
                await _resetConnection.DisposeAsync();
            }

            await _database.DisposeAsync();
            await base.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                Replace<ICacheService>(services, Cache);
                Replace<IEmailSender>(services, EmailSender);
                Replace<IFileStorage>(services, FileStorage);
                Replace<INotificationSender>(services, NotificationSender);
                Replace<IPaymentProvider>(services, PaymentProvider);
                Replace<IBackgroundJobScheduler>(services, BackgroundJobs);
                Replace<IOAuthStateStore>(services, OAuthStateStore);
                Replace<IPendingRegistrationStore>(services, PendingRegistrations);

                // The external auth providers are registered as a multi-binding (Google + GitHub) behind a
                // factory, so swap the whole set.
                services.RemoveAll<IExternalAuthProvider>();
                services.RemoveAll<IExternalAuthProviderFactory>();
                services.AddSingleton<IExternalAuthProvider>(GoogleAuth);
                services.AddSingleton<IExternalAuthProvider>(GitHubAuth);
                services.AddSingleton<IExternalAuthProviderFactory, FakeExternalAuthProviderFactory>();

                // Give each test its own rate-limit partition (see TestClientIpStartupFilter).
                services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>();
            });
        }

        private static void Replace<TService>(IServiceCollection services, TService instance)
            where TService : class
        {
            services.RemoveAll<TService>();
            services.AddSingleton(instance);
        }

        /// <summary>
        /// Truncates all data in the application schema between tests. The seeded reference data
        /// (consent documents) and the EF migrations history are preserved; everything else is reset so
        /// each test starts from a known-empty state. Also flushes the in-memory cache.
        /// </summary>
        public async Task ResetDatabaseAsync()
        {
            // Touching Services forces the host to build & start (running migrations + seeders) the first time.
            _ = Services;

            if (_respawner is null)
            {
                _resetConnection = new NpgsqlConnection(_database.GetConnectionString());
                await _resetConnection.OpenAsync();
                _respawner = await Respawner.CreateAsync(_resetConnection, new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,
                    SchemasToInclude = ["public"],
                    TablesToIgnore =
                    [
                        new Table("__EFMigrationsHistory"),
                        new Table("consent_documents"),
                    ],
                });
            }

            await _respawner.ResetAsync(_resetConnection!);

            // The benchmark table is truncated with everything else (write isolation between tests), so
            // restore the initial median seed the engine relies on — mirrors ValuationBenchmarksSeeder.
            await SeedValuationBenchmarksAsync();

            Cache.Clear();
        }

        private async Task SeedValuationBenchmarksAsync()
        {
            using IServiceScope scope = Services.CreateScope();
            ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (await db.ValuationBenchmarks.AnyAsync())
            {
                return;
            }

            var effectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            (StartupStage Stage, decimal Value)[] medians =
            [
                (StartupStage.Idea, 60_000_000m),
                (StartupStage.PreSeed, 120_000_000m),
                (StartupStage.Mvp, 250_000_000m),
                (StartupStage.Seed, 400_000_000m),
            ];

            foreach ((StartupStage stage, decimal value) in medians)
            {
                db.ValuationBenchmarks.Add(ValuationBenchmark.Create(
                    BenchmarkMetricType.PreMoneyMedian, Industry.Other, stage, value,
                    currency: "RUB", effectiveFrom: effectiveFrom, source: "initial seed",
                    createdByUserId: null, utcNow: effectiveFrom));
            }

            await db.SaveChangesAsync();
        }
    }
}
