using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Configuration;
using DevStart.Domain.Security;
using DevStart.Domain.Users;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevStart.Application.Users.Security.UpdateSecuritySettings
{
    internal sealed class UpdateSecuritySettingsCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IUserSecuritySettingsProvider securitySettings,
        ITrustedDeviceService trustedDevices,
        IOptions<TrustedDeviceOptions> options,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateSecuritySettingsCommand>
    {
        public async Task<Result> Handle(UpdateSecuritySettingsCommand command, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            User? user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null)
            {
                return Result.Failure(UserErrors.NotFound(userId));
            }

            UserSecuritySettings settings = await securitySettings.GetOrCreateAsync(userId, cancellationToken);

            // The validator only knows the presets; the cap depends on the caller's role, so clamp here.
            int cap = SecurityTrustDuration.CapFor(user, options.Value);
            int days = Math.Clamp(Math.Min(command.TrustDurationDays, cap), 1, cap);

            bool trustPolicyChanged = settings.Update(
                command.Strictness, days, command.NotifyOnNewDeviceLogin, dateTimeProvider.UtcNow);

            await context.SaveChangesAsync(cancellationToken);

            // Devices trusted under the old policy were granted on different terms — a shortened
            // duration or a stricter level must not leave them standing. Toggling only the email
            // preference deliberately does not reach here.
            if (trustPolicyChanged)
            {
                await trustedDevices.RevokeAllForUserAsync(userId, cancellationToken);
            }

            return Result.Success();
        }
    }
}
