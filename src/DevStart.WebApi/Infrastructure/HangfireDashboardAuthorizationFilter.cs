using System.Security.Cryptography;
using System.Text;
using DevStart.Application.Abstractions.Authorization;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;

namespace DevStart.WebApi.Infrastructure
{
    // The Hangfire dashboard is browser-navigated, but the API is header-based JWT (no cookies, no
    // role claim — permissions are resolved per-request from the DB). A plain browser GET therefore
    // never carries a bearer token, so this filter is deny-by-default with three explicit allow paths:
    //
    //   1. Development — always open, matching prior behaviour.
    //   2. Trusted reverse proxy — nginx terminates Basic Auth / IP-allowlist and stamps a secret
    //      header the client cannot forge (nginx overwrites any client-supplied value). This is the
    //      real production gate for human/browser access.
    //   3. Authenticated admin principal — covers access with a bearer token (tooling/automation),
    //      evaluated through the same permission pipeline endpoints use (IAuthorizationService).
    //
    // Async filter (DashboardOptions.AsyncAuthorization) so the policy evaluation in path 3 — which
    // resolves permissions from the DB — is awaited instead of blocking a thread-pool thread.
    internal sealed class HangfireDashboardAuthorizationFilter : IDashboardAsyncAuthorizationFilter
    {
        private readonly IHostEnvironment _environment;
        private readonly string _headerName;
        // Pre-encoded once (the filter lives for the app's lifetime) so the per-request comparison
        // does not re-read configuration or re-allocate the secret bytes. Null = proxy path disabled.
        private readonly byte[]? _secretBytes;

        public HangfireDashboardAuthorizationFilter(IHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _headerName = configuration["Hangfire:Dashboard:ProxyAuthHeaderName"] ?? "X-Hangfire-Auth";

            string? secret = configuration["Hangfire:Dashboard:ProxyAuthSecret"];
            _secretBytes = string.IsNullOrWhiteSpace(secret) ? null : Encoding.UTF8.GetBytes(secret.Trim());
        }

        public async Task<bool> AuthorizeAsync(DashboardContext context)
        {
            if (_environment.IsDevelopment())
            {
                return true;
            }

            HttpContext httpContext = context.GetHttpContext();

            if (HasValidProxyAuthHeader(httpContext))
            {
                return true;
            }

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                IAuthorizationService authorizationService =
                    httpContext.RequestServices.GetRequiredService<IAuthorizationService>();

                AuthorizationResult result = await authorizationService
                    .AuthorizeAsync(httpContext.User, Permissions.AdminObservabilityRead);

                return result.Succeeded;
            }

            return false;
        }

        private bool HasValidProxyAuthHeader(HttpContext httpContext)
        {
            if (_secretBytes is null)
            {
                return false;
            }

            if (!httpContext.Request.Headers.TryGetValue(_headerName, out var provided) || provided.Count == 0)
            {
                return false;
            }

            // Last value wins if the header is repeated (nginx's proxy_set_header replaces rather than
            // appends, so duplicates only occur from noise); trim to tolerate stray whitespace.
            string? value = provided[^1]?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(value), _secretBytes);
        }
    }
}
