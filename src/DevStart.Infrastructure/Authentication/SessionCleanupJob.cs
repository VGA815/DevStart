using DevStart.Application.Abstractions.Data;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevStart.Infrastructure.Authentication
{
    /// <summary>
    /// Prunes dead auth rows, which nothing did before. Retention windows — and the reason refresh
    /// tokens outlive everything else — live in <see cref="SessionRetentionPolicy"/>.
    /// </summary>
    public sealed class SessionCleanupJob(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ILogger<SessionCleanupJob> logger)
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            DateTime now = dateTimeProvider.UtcNow;
            DateTime refreshCutoff = now - SessionRetentionPolicy.RefreshTokenRetention;
            DateTime deviceCutoff = now - SessionRetentionPolicy.TrustedDeviceRetention;

            int refreshTokens = await context.RefreshTokens
                .Where(t => t.ExpiresAt < refreshCutoff
                    || (t.RevokedAt != null && t.RevokedAt < refreshCutoff))
                .ExecuteDeleteAsync(cancellationToken);

            int devices = await context.TrustedDevices
                .Where(d => d.ExpiresAt < deviceCutoff
                    || (d.RevokedAt != null && d.RevokedAt < deviceCutoff))
                .ExecuteDeleteAsync(cancellationToken);

            if (refreshTokens > 0 || devices > 0)
            {
                logger.LogInformation(
                    "Session cleanup removed {RefreshTokenCount} refresh token(s) and {DeviceCount} trusted device(s).",
                    refreshTokens, devices);
            }
        }
    }
}
