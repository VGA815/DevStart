using DevStart.Application.Abstractions.Authorization;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevStart.WebApi.Extensions
{
    public static class HealthCheckEndpointExtensions
    {
        // Three probes with distinct audiences:
        //  - /health/live   : liveness — process is up; no dependency checks (orchestrator restart signal).
        //  - /health/ready  : readiness — critical dependencies; terse body, safe to expose publicly.
        //  - /health/details: full diagnostics incl. exception text; admin-only.
        public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
        {
            app.MapHealthChecks("health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            app.MapHealthChecks("health/ready", new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready"),
                ResponseWriter = WriteTerseResponse
            });

            app.MapHealthChecks("health/details", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            }).RequireAuthorization(Permissions.AdminObservabilityRead);

            // Backwards-compatible alias for the original single endpoint.
            app.MapHealthChecks("health", new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready"),
                ResponseWriter = WriteTerseResponse
            });

            return app;
        }

        // Deliberately omits descriptions/exception details/connection data — this body is public.
        private static Task WriteTerseResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var payload = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString()
                })
            };

            return context.Response.WriteAsJsonAsync(payload);
        }
    }
}
