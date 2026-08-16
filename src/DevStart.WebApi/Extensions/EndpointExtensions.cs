using DevStart.WebApi.Endpoints;
using DevStart.WebApi.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace DevStart.WebApi.Extensions
{
    public static class EndpointExtensions
    {
        public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
        {
            ServiceDescriptor[] serviceDescriptors = assembly
                .DefinedTypes
                .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                               type.IsAssignableTo(typeof(IEndpoint)))
                .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
                .ToArray();

            services.TryAddEnumerable(serviceDescriptors);

            return services;
        }

        public static IApplicationBuilder MapEndpoints(
            this WebApplication app,
            RouteGroupBuilder? routeGroupBuilder = null)
        {
            IEnumerable<IEndpoint> endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

            IEndpointRouteBuilder builder = routeGroupBuilder is null ? app : routeGroupBuilder;

            foreach (IEndpoint endpoint in endpoints)
            {
                endpoint.MapEndpoint(builder);
            }

            return app;
        }

        public static RouteHandlerBuilder HasPermission(this RouteHandlerBuilder app, string permission)
        {
            return app.RequireAuthorization(permission);
        }

        /// <summary>
        /// Requires a valid Yandex SmartCaptcha token in the X-Captcha-Token header. A no-op when
        /// Captcha:Enabled is false. Declare it alongside RequireRateLimiting("auth") — the two are
        /// complementary: the limiter caps volume per IP, the captcha raises the per-request cost.
        /// </summary>
        public static RouteHandlerBuilder RequireCaptcha(this RouteHandlerBuilder app)
        {
            return app
                .AddEndpointFilter<CaptchaEndpointFilter>()
                .WithMetadata(new RequiresCaptchaMetadata());
        }
    }
}
