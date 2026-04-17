using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Notifications;
using DevStart.Infrastructure.Authentication;
using DevStart.Infrastructure.Authorization;
using DevStart.Infrastructure.Caching;
using DevStart.Infrastructure.Database;
using DevStart.Infrastructure.DomainEvents;
using DevStart.Infrastructure.FileStorage;
using DevStart.Infrastructure.Notifications;
using DevStart.Infrastructure.Time;
using DevStart.SharedKernel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                .AddAuthorizationInternal();
        private static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

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
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.RequireHttpsMetadata = false;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],
                        ClockSkew = TimeSpan.Zero,
                    };
                });

            services.AddHttpContextAccessor();
            services.AddScoped<IUserContext, UserContext>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<ITokenProvider, TokenProvider>();

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
            return services;
        }
    }
}
