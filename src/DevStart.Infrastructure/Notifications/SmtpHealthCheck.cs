using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DevStart.Infrastructure.Notifications
{
    // Email is non-critical to serving traffic, so this is tagged "details" (not "ready") and reports
    // Degraded rather than Unhealthy. A short TCP connect to the SMTP host:port is enough to tell
    // whether outbound mail delivery is reachable without authenticating or sending anything.
    internal sealed class SmtpHealthCheck(IConfiguration configuration) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            string? host = configuration["Smtp:Host"];
            if (string.IsNullOrWhiteSpace(host) || !int.TryParse(configuration["Smtp:Port"], out int port))
            {
                return HealthCheckResult.Degraded("SMTP is not configured.");
            }

            try
            {
                using var client = new TcpClient();
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));

                await client.ConnectAsync(host, port, timeout.Token);

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Degraded($"SMTP host {host}:{port} is unreachable.", ex);
            }
        }
    }
}
