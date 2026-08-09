using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Users.Security.GetSecuritySettings
{
    public sealed record GetSecuritySettingsQuery : IQuery<SecuritySettingsResponse>;

    /// <summary>
    /// <paramref name="MaxTrustDurationDays"/> is the cap already resolved for this user (admins get a
    /// shorter one) and <paramref name="AvailableDurations"/> is the preset list filtered by it, so the
    /// client never needs to know about roles or presets.
    /// </summary>
    public sealed record SecuritySettingsResponse(
        int Strictness,
        int TrustDurationDays,
        bool NotifyOnNewDeviceLogin,
        int MaxTrustDurationDays,
        IReadOnlyList<int> AvailableDurations);
}
