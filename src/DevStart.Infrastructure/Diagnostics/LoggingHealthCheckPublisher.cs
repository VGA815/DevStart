using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace DevStart.Infrastructure.Diagnostics
{
    // Bridges the periodic health-check evaluation to structured logs so Seq can raise alerts.
    // Each unhealthy/degraded entry is logged with stable structured properties (HealthCheckName,
    // HealthStatus) that a Seq signal/alert can key off — e.g. "@Level in ['Error','Warning'] and
    // HealthCheckName is not null". Healthy reports are logged at Debug to avoid noise.
    internal sealed class LoggingHealthCheckPublisher(ILogger<LoggingHealthCheckPublisher> logger)
        : IHealthCheckPublisher
    {
        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            if (report.Status == HealthStatus.Healthy)
            {
                logger.LogDebug("Health report is {HealthStatus}", report.Status);
                return Task.CompletedTask;
            }

            foreach ((string name, HealthReportEntry entry) in report.Entries)
            {
                if (entry.Status == HealthStatus.Healthy)
                {
                    continue;
                }

                LogLevel level = entry.Status == HealthStatus.Unhealthy ? LogLevel.Error : LogLevel.Warning;

                logger.Log(
                    level,
                    entry.Exception,
                    "Health check {HealthCheckName} is {HealthStatus}: {HealthDescription}",
                    name,
                    entry.Status,
                    entry.Description ?? "(no description)");
            }

            return Task.CompletedTask;
        }
    }
}
