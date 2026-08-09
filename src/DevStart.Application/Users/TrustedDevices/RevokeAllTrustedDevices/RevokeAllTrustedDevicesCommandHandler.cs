using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;

namespace DevStart.Application.Users.TrustedDevices.RevokeAllTrustedDevices
{
    internal sealed class RevokeAllTrustedDevicesCommandHandler(
        IUserContext userContext,
        ITrustedDeviceService trustedDevices) : ICommandHandler<RevokeAllTrustedDevicesCommand>
    {
        public async Task<Result> Handle(RevokeAllTrustedDevicesCommand command, CancellationToken cancellationToken)
        {
            // Only the device trust is dropped — sessions stay, since the user asked to stop skipping
            // the second factor, not to be signed out everywhere.
            await trustedDevices.RevokeAllForUserAsync(userContext.UserId, cancellationToken);
            return Result.Success();
        }
    }
}
