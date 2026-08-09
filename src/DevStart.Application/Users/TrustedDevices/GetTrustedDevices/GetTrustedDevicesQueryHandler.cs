using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Auth.Sessions;
using DevStart.Domain.TrustedDevices;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.TrustedDevices.GetTrustedDevices
{
    internal sealed class GetTrustedDevicesQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider)
        : IQueryHandler<GetTrustedDevicesQuery, IReadOnlyList<TrustedDeviceResponse>>
    {
        public async Task<Result<IReadOnlyList<TrustedDeviceResponse>>> Handle(
            GetTrustedDevicesQuery query, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;
            DateTime now = dateTimeProvider.UtcNow;

            List<TrustedDevice> devices = await context.TrustedDevices
                .AsNoTracking()
                .Where(d => d.UserId == userId && d.RevokedAt == null && d.ExpiresAt > now)
                .OrderByDescending(d => d.LastUsedAt)
                .ToListAsync(cancellationToken);

            IReadOnlyList<TrustedDeviceResponse> response = [.. devices.Select(d =>
            {
                // Label is materialized at mint time; parse the stored UA only to fill the columns
                // the UI shows separately.
                UserAgentInfo ua = UserAgentParser.Parse(d.UserAgent);
                return new TrustedDeviceResponse(
                    d.Id,
                    d.Label ?? ua.Label,
                    ua.Browser,
                    ua.Os,
                    d.CreatedAt,
                    d.LastUsedAt,
                    d.ExpiresAt,
                    d.LastSeenIp ?? d.CreatedByIp);
            })];

            return Result.Success(response);
        }
    }
}
