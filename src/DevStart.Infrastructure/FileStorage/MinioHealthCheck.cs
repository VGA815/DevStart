using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;

namespace DevStart.Infrastructure.FileStorage
{
    // Lightweight liveness probe for MinIO: a single ListBuckets round-trip verifies both network
    // reachability and credential validity without depending on any particular bucket existing.
    internal sealed class MinioHealthCheck(IMinioClient client) : IHealthCheck
    {

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await client.ListBucketsAsync(cancellationToken);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("MinIO object storage is unreachable.", ex);
            }
        }
    }
}
