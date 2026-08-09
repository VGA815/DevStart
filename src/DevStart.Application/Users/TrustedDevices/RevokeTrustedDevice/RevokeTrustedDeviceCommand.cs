using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.TrustedDevices.RevokeTrustedDevice
{
    public sealed record RevokeTrustedDeviceCommand(Guid DeviceId) : ICommand;
}
