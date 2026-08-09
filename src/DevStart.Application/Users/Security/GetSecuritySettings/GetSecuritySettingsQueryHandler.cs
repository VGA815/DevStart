using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Configuration;
using DevStart.Domain.Security;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.Application.Users.Security.GetSecuritySettings
{
    internal sealed class GetSecuritySettingsQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IUserSecuritySettingsProvider securitySettings,
        IOptions<TrustedDeviceOptions> options)
        : IQueryHandler<GetSecuritySettingsQuery, SecuritySettingsResponse>
    {
        public async Task<Result<SecuritySettingsResponse>> Handle(
            GetSecuritySettingsQuery query, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            User? user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
            {
                return Result.Failure<SecuritySettingsResponse>(UserErrors.NotFound(userId));
            }

            UserSecuritySettings settings = await securitySettings.GetOrDefaultAsync(userId, cancellationToken);

            int cap = SecurityTrustDuration.CapFor(user, options.Value);
            IReadOnlyList<int> available = SecurityTrustDuration.AvailableDurations(cap);

            return new SecuritySettingsResponse(
                (int)settings.Strictness,
                Math.Min(settings.TrustDurationDays, cap),
                settings.NotifyOnNewDeviceLogin,
                cap,
                available);
        }
    }
}
