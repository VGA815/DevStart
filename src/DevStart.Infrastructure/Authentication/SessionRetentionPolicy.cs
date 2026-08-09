namespace DevStart.Infrastructure.Authentication
{
    /// <summary>
    /// How long dead auth rows are kept, and how far back a login counts as "from a device we've seen".
    ///
    /// These two numbers are coupled: the new-device email decides whether a browser/OS pair is
    /// familiar by looking at the user's own <c>refresh_tokens</c> history, so if cleanup ever started
    /// deleting rows the lookback still wants, every user would begin getting "new device" warnings
    /// for browsers they use daily. They live together here — with a test asserting the ordering —
    /// rather than as two constants in two files that quietly drift apart.
    /// </summary>
    internal static class SessionRetentionPolicy
    {
        /// <summary>How far back a browser/OS pair still counts as already seen.</summary>
        public static readonly TimeSpan KnownDeviceLookback = TimeSpan.FromDays(90);

        /// <summary>
        /// How long an expired or revoked refresh token is kept before deletion. Must stay comfortably
        /// above <see cref="KnownDeviceLookback"/> — that history is what makes the lookback work.
        /// </summary>
        public static readonly TimeSpan RefreshTokenRetention = TimeSpan.FromDays(180);

        /// <summary>Trusted devices carry no such history, so they can go sooner.</summary>
        public static readonly TimeSpan TrustedDeviceRetention = TimeSpan.FromDays(90);
    }
}
