using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.TrustedDevices;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Users.TrustedDevices.RevokeTrustedDevice
{
    internal sealed class RevokeTrustedDeviceCommandHandler(
        IApplicationDbContext context,
        IUserContext userContext,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<RevokeTrustedDeviceCommand>
    {
        public async Task<Result> Handle(RevokeTrustedDeviceCommand command, CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            // Scoped to the caller; someone else's device id is NotFound, not Forbidden.
            TrustedDevice? device = await context.TrustedDevices
                .FirstOrDefaultAsync(d => d.Id == command.DeviceId && d.UserId == userId, cancellationToken);

            if (device is null)
            {
                return Result.Failure(TrustedDeviceErrors.NotFound);
            }

            if (device.IsRevoked)
            {
                return Result.Success();
            }

            device.Revoke(dateTimeProvider.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
