using DevStart.SharedKernel;

namespace DevStart.Domain.TrustedDevices
{
    public static class TrustedDeviceErrors
    {
        public static readonly Error NotFound = Error.NotFound(
            "TrustedDevices.NotFound",
            "The trusted device was not found");
    }
}
