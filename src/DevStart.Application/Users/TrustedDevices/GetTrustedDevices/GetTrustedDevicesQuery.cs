using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.TrustedDevices.GetTrustedDevices
{
    public sealed record GetTrustedDevicesQuery : IQuery<IReadOnlyList<TrustedDeviceResponse>>;

    /// <summary>
    /// No "current" flag: the client stored the device id alongside the token when it was minted and
    /// marks its own row locally, which costs the server nothing.
    /// </summary>
    public sealed record TrustedDeviceResponse(
        Guid Id,
        string? Label,
        string Browser,
        string Os,
        DateTime CreatedAt,
        DateTime LastUsedAt,
        DateTime ExpiresAt,
        string? IpAddress);
}
