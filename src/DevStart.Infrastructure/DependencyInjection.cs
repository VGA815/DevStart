using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.BackgroundJobs;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Application.Abstractions.Payments;
using DevStart.Application.Abstractions.Subscriptions;
using DevStart.Application.Configuration;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Application.Subscriptions;
using DevStart.Infrastructure.Authentication;
using DevStart.Infrastructure.Authentication.OAuth;
using DevStart.Infrastructure.Authentication.RefreshTokens;
using DevStart.Infrastructure.Authorization;
using DevStart.Infrastructure.BackgroundJobs;
using DevStart.Infrastructure.Caching;
using DevStart.Infrastructure.Database;
using DevStart.Infrastructure.ConsentDocuments;
using DevStart.Infrastructure.DealDocuments;
using DevStart.Infrastructure.DealDocuments.Generation;
using DevStart.Infrastructure.DomainEvents;
using DevStart.Infrastructure.FileStorage;
using DevStart.Infrastructure.Moderation;
using DevStart.Infrastructure.Notifications;
using DevStart.Infrastructure.Payments;
using DevStart.Infrastructure.Subscriptions;
using DevStart.Infrastructure.Time;
using DevStart.Infrastructure.Valuation;
using DevStart.SharedKernel;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Net.Mail;
using System.Text;

namespace DevStart.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration) =>
            services
                .AddServices()
                .AddDatabase(configuration)
                .AddFileStorage(configuration)
                .AddCaching(configuration)
                .AddHealthChecks(configuration)
                .AddAuthenticationInternal(configuration)
                .AddCentrifugo(configuration)
                .AddSmtp(configuration)
                .AddAuthorizationInternal()
                .AddBackgroundJobs(configuration)
                .AddDealDocumentGeneration()
                .AddBilling(configuration)
                .AddValuation(configuration);
        private static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

            return services;
        }

        // Binds the tunable valuation constants (Berkus ceilings, VC multiples/IRR, range band,
        // methodology version) over the code defaults registered in AddApplication, and wires the
        // database-backed benchmark provider (medians + revenue multiples) plus its initial seed.
        private static IServiceCollection AddValuation(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<Application.Scoring.ValuationOptions>(
                configuration.GetSection(Application.Scoring.ValuationOptions.SectionName));

            services.AddScoped<Application.Scoring.IValuationBenchmarkProvider, ValuationBenchmarkProvider>();
            services.AddHostedService<ValuationBenchmarksSeeder>();

            return services;
        }
        private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("Database");

            services.AddDbContext<ApplicationDbContext>(
                options => options
                    .UseNpgsql(connectionString, npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default))
                    .UseSnakeCaseNamingConvention());

            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

            return services;
        }
        private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddHealthChecks()
                .AddNpgSql(configuration.GetConnectionString("Database")!);

            return services;
        }
        private static IServiceCollection AddAuthenticationInternal(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            string? jwtSecret = configuration["Jwt:Secret"];
            if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Secret is missing or too short. Configure a secret of at least 32 characters (recommended) " +
                    "for HS256, e.g. via the Jwt__Secret environment variable.");
            }

            bool requireHttpsMetadata = configuration.GetValue("Jwt:RequireHttpsMetadata", true);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.RequireHttpsMetadata = requireHttpsMetadata;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],
                        ClockSkew = TimeSpan.Zero,
                    };
                });

            services.AddHttpContextAccessor();
            services.AddScoped<IUserContext, UserContext>();

            // Base URL of the SPA — used to redirect email-verification clicks to a friendly page.
            services.Configure<FrontendOptions>(configuration.GetSection("Frontend"));
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<ITokenProvider, TokenProvider>();

            services.Configure<RefreshTokenOptions>(configuration.GetSection("Jwt:RefreshToken"));
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();

            services.AddSingleton<IPkceGenerator, PkceGenerator>();
            services.AddSingleton<IOAuthStateStore, RedisOAuthStateStore>();
            services.AddSingleton<IPendingRegistrationStore, RedisPendingRegistrationStore>();

            services.AddOptions<GoogleOAuthOptions>()
                .Bind(configuration.GetSection("OAuth:Google"))
                .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId), "OAuth:Google:ClientId is required")
                .Validate(o => !string.IsNullOrWhiteSpace(o.ClientSecret), "OAuth:Google:ClientSecret is required")
                .ValidateOnStart();

            services.AddOptions<GitHubOAuthOptions>()
                .Bind(configuration.GetSection("OAuth:GitHub"))
                .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId), "OAuth:GitHub:ClientId is required")
                .Validate(o => !string.IsNullOrWhiteSpace(o.ClientSecret), "OAuth:GitHub:ClientSecret is required")
                .ValidateOnStart();

            services.AddHttpClient<GoogleAuthProvider>();
            services.AddHttpClient<GitHubAuthProvider>();

            services.AddScoped<IExternalAuthProvider>(sp => sp.GetRequiredService<GoogleAuthProvider>());
            services.AddScoped<IExternalAuthProvider>(sp => sp.GetRequiredService<GitHubAuthProvider>());
            services.AddScoped<IExternalAuthProviderFactory, ExternalAuthProviderFactory>();

            return services;
        }
        private static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
        {
            services.AddAuthorization();

            services.AddScoped<PermissionProvider>();

            services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

            services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

            return services;
        }
        private static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MinioOptions>(
                    configuration.GetSection("Minio"));

            services.AddSingleton<IFileStorage, MinioFileStorage>();

            return services;
        }
        private static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RedisOptions>(
                configuration.GetSection("Redis"));

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
                return ConnectionMultiplexer.Connect(options.ConnectionString);
            });

            services.AddSingleton<ICacheService, RedisCacheService>();

            return services;
        }
        private static IServiceCollection AddSmtp(this IServiceCollection services, IConfiguration configuration)
        {
            var smtp = new SmtpClient
            {
                Host = configuration["Smtp:Host"]!,
                Port = int.Parse(configuration["Smtp:Port"]!),
                EnableSsl = bool.Parse(configuration["Smtp:EnableSsl"]!),
                UseDefaultCredentials = bool.Parse(configuration["Smtp:UseDefaultCredentials"]!),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new System.Net.NetworkCredential(
                    configuration["Smtp:Username"],
                    configuration["Smtp:Password"])
            };

            services.AddFluentEmail(configuration["Smtp:Username"]!)
                .AddSmtpSender(smtp);

            services.AddScoped<IEmailSender, EmailSender>();

            return services;
        }
        private static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("Database");

            services.AddHangfire(cfg => cfg
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(connectionString!)));

            services.AddHangfireServer();

            services.AddScoped<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
            services.AddScoped<TermSheetGenerationJob>();
            services.AddScoped<BanExpiryJob>();

            return services;
        }

        private static IServiceCollection AddDealDocumentGeneration(this IServiceCollection services)
        {
            services.AddScoped<ITermSheetGenerator, TermSheetGenerator>();
            services.AddHostedService<TemplatesSeeder>();
            services.AddHostedService<ConsentDocumentsSeeder>();
            return services;
        }

        private static IServiceCollection AddBilling(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<YooKassaOptions>()
                .Bind(configuration.GetSection("YooKassa"))
                .Validate(o => !string.IsNullOrWhiteSpace(o.ShopId), "YooKassa:ShopId is required")
                .Validate(o => !string.IsNullOrWhiteSpace(o.SecretKey), "YooKassa:SecretKey is required")
                .Validate(o => !string.IsNullOrWhiteSpace(o.ReturnUrl), "YooKassa:ReturnUrl is required")
                .Validate(o => Uri.TryCreate(o.ApiUrl, UriKind.Absolute, out _), "YooKassa:ApiUrl must be an absolute URL")
                .ValidateOnStart();

            services.Configure<CheckoutOptions>(configuration.GetSection("YooKassa"));
            services.Configure<YooKassaReceiptOptions>(configuration.GetSection("YooKassa:Receipt"));
            services.Configure<PlansOptions>(configuration.GetSection("Plans"));
            services.Configure<BillingMaintenanceOptions>(configuration.GetSection("Billing"));

            services.AddTransient<YooKassaResilienceHandler>();
            services.AddHttpClient<IPaymentProvider, YooKassaPaymentProvider>(client =>
                    client.Timeout = TimeSpan.FromSeconds(60))
                .AddHttpMessageHandler<YooKassaResilienceHandler>();

            services.AddScoped<ISubscriptionChecker, SubscriptionChecker>();

            // Recurring billing jobs (reconciliation of stuck payments + renewal reminders/expiry).
            services.AddScoped<PaymentReconciliationJob>();
            services.AddScoped<SubscriptionMaintenanceJob>();
            services.AddHostedService<RecurringJobsRegistrar>();

            return services;
        }

        private static IServiceCollection AddCentrifugo(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CentrifugoOptions>(
                configuration.GetSection("Centrifugo"));
            services.AddHttpClient("centrifugo", (sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<CentrifugoOptions>>().Value;
                client.BaseAddress = new Uri(options.ApiUrl);
                client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
            });
            services.AddScoped<INotificationSender, CentrifugoNotificationSender>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddSingleton<ICentrifugoTokenProvider, CentrifugoTokenProvider>();
            return services;
        }
    }
}
