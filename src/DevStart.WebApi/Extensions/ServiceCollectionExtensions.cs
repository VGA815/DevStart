using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace DevStart.WebApi.Extensions
{
    internal static  class ServiceCollectionExtensions
    {
        internal static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services)
        {
            services.AddSwaggerGen(o =>
            {
                o.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "JWT Authentication",
                    Description = "Enter your JWT token in this field",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT"
                };

                o.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);


                o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
                
            });

            return services;
        }
        internal static IServiceCollection AddRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, token) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = $"{retryAfter.TotalSeconds}";

                        ProblemDetailsFactory problemDetailsFactory = context.HttpContext.RequestServices
                            .GetRequiredService<ProblemDetailsFactory>();
                        Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails = problemDetailsFactory
                            .CreateProblemDetails(
                                context.HttpContext,
                                StatusCodes.Status429TooManyRequests,
                                "Too Many Requests",
                                detail: $"Too many requests. Please try again after {retryAfter.TotalSeconds} seconds.");

                        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: token);
                    }
                };

                options.AddFixedWindowLimiter("fixed", cfg =>
                {
                    cfg.PermitLimit = 10;
                    cfg.Window = TimeSpan.FromMinutes(1);
                });

                options.AddPolicy("per-user", httpContext =>
                {
                    string? userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (!string.IsNullOrWhiteSpace(userId))
                    {
                        return RateLimitPartition.GetTokenBucketLimiter(
                            userId,
                            _ => new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = 10,
                                TokensPerPeriod = 5,
                                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                            });
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(
                        "anonymous",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                        });
                });
            });

            return services;
        }
    }
}
