using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevStart.Infrastructure.Notifications
{
    // Probes Centrifugo's built-in /health endpoint via the shared named HttpClient (base address and
    // API key are already configured in DI), so realtime delivery outages surface in readiness.
    internal sealed class CentrifugoHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                HttpClient client = httpClientFactory.CreateClient("centrifugo");

                using HttpResponseMessage response = await client.GetAsync("/health", cancellationToken);

                return response.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy(
                        $"Centrifugo /health returned {(int)response.StatusCode}.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Centrifugo is unreachable.", ex);
            }
        }
    }
}
